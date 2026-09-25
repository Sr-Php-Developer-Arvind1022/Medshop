using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.WhatsApp.BackgroundServices;

public class LowStockWhatsAppAlertService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LowStockWhatsAppAlertService> _logger;
    private readonly IWebHostEnvironment _environment;

    public LowStockWhatsAppAlertService(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LowStockWhatsAppAlertService> logger,
        IWebHostEnvironment environment)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _configuration.GetValue<int?>("LowStockAlert:RunAt") ?? 180;
        var interval = intervalMinutes > 0 ? TimeSpan.FromMinutes(intervalMinutes) : TimeSpan.FromMinutes(5);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendLowStockAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing low-stock WhatsApp alerts.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task CheckAndSendLowStockAlertsAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue<bool>("LowStockAlert:Enabled", true))
        {
            _logger.LogInformation("Low stock WhatsApp alert job is disabled.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MedshopDbContext>();

        var threshold = _configuration.GetValue<int>("LowStockAlert:Threshold", 10);
        var templateCode = _configuration["LowStockAlert:TemplateCode"]
            ?? _configuration["WhatsApp:TemplateCode"]
            ?? "LOW_QUANTITY_PRODUCT";

        // Every user with a mobile number is a candidate recipient; each one only gets
        // alerted about their own products (matched via Product.LoginId == User.Id).
        var users = await dbContext.Users
            .Where(u => !string.IsNullOrWhiteSpace(u.Mobile))
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            _logger.LogInformation("No users with a mobile number configured. Low-stock alert skipped.");
            return;
        }

        foreach (var user in users)
        {
            await ProcessLowStockAlertForUserAsync(dbContext, user, threshold, templateCode, cancellationToken);
        }
    }

    private async Task ProcessLowStockAlertForUserAsync(
        MedshopDbContext dbContext,
        User user,
        int threshold,
        string templateCode,
        CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .Where(p => !p.IsDeleted && p.StockQuantity < threshold && p.LoginId == user.Id)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new { p.Name, p.StockQuantity })
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            // Nothing low on stock for this particular user - skip silently, this is the normal case.
            return;
        }

        var recipientPhone = user.Mobile;
        var apiKey = user.WhatsAppApiKey ?? _configuration["WhatsApp:ApiKey"];

        if (string.IsNullOrWhiteSpace(recipientPhone))
        {
            _logger.LogWarning("No recipient phone found for user {UserId}. Low-stock alert skipped.", user.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("WhatsApp API key missing for user {UserId}. Low-stock alert skipped.", user.Id);
            return;
        }

        var mediaUrl = await GenerateLowStockPdfReportAsync(
            products.Select(p => (p.Name, p.StockQuantity)).ToList(),
            cancellationToken,
            user.Id);

        var ownerName = !string.IsNullOrWhiteSpace(user.OwnerName)
            ? user.OwnerName
            : (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : "Customer");
        var productSummary = BuildTruncatedProductSummary(products.Select(p => p.Name), maxLength: 120);

        // The WapHub template requires "name", "items", "amount" and "date" for validation, but the
        // rendered message body appears to use "products" / "count" placeholders. We send both sets
        // so validation passes AND the visible text actually fills in.
        var variables = !string.IsNullOrWhiteSpace(mediaUrl)
            ? new Dictionary<string, object>
            {
                ["name"] = ownerName,
                ["items"] = "Test",
                ["amount"] = "0",
                ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["products"] = productSummary,
                ["count"] = products.Count.ToString()
            }
            : new Dictionary<string, object>
            {
                ["name"] = ownerName,
                ["items"] = productSummary,
                ["amount"] = products.Count.ToString(),
                ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["products"] = productSummary,
                ["count"] = products.Count.ToString()
            };

        var payload = new Dictionary<string, object?>
        {
            ["recipient_phone"] = recipientPhone,
            ["template_code"] = templateCode,
            ["variables"] = variables
        };

        if (!string.IsNullOrWhiteSpace(mediaUrl))
        {
            payload["media_url"] = mediaUrl;
        }
        else
        {
            _logger.LogWarning(
                "Low-stock PDF report was not generated for user {UserId}; sending alert without media_url.",
                user.Id);
        }

        var client = _httpClientFactory.CreateClient("WapHub");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/messages/send-template");
        request.Content = JsonContent.Create(payload);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Low-stock WhatsApp alert failed for user {UserId}. StatusCode: {StatusCode}. Response: {ResponseBody}",
                    user.Id,
                    (int)response.StatusCode,
                    responseBody);
                return;
            }

            _logger.LogInformation(
                "Low-stock WhatsApp alert sent successfully to {RecipientPhone} (user {UserId}) for {ProductCount} product(s).",
                recipientPhone,
                user.Id,
                products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending low-stock WhatsApp alert for user {UserId}.", user.Id);
        }
    }

    /// <summary>
    /// Generates a low-stock PDF report, saves it under wwwroot/reports (served as static files),
    /// and returns a publicly reachable URL built from App:PublicBaseUrl. Returns null on failure
    /// so the caller can still send the WhatsApp alert without an attachment.
    ///
    /// The PDF is hand-built as raw PDF syntax using only the built-in "Helvetica" standard font
    /// (one of the 14 fonts every PDF reader already supports natively). This means no font files,
    /// no NuGet PDF library, and no dependency on fonts being installed in the deployment container.
    /// </summary>
    private async Task<string?> GenerateLowStockPdfReportAsync(
        IReadOnlyList<(string Name, int StockQuantity)> products,
        CancellationToken cancellationToken,
        Guid? userId = null)
    {
        try
        {
            var publicBaseUrl = _configuration["App:PublicBaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(publicBaseUrl))
            {
                _logger.LogWarning(
                    "App:PublicBaseUrl is not configured; cannot build a public media_url for the PDF report.");
                return null;
            }

            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                _logger.LogWarning("WebRootPath is not available; cannot save the PDF report for static serving.");
                return null;
            }

            var reportsDirectory = Path.Combine(webRoot, "reports");
            Directory.CreateDirectory(reportsDirectory);

            var userSuffix = userId is null ? string.Empty : $"-{userId:N}";
            var fileName = $"low-stock-{DateTime.UtcNow:yyyyMMdd-HHmmss}{userSuffix}.pdf";
            var filePath = Path.Combine(reportsDirectory, fileName);

            var pdfBytes = BuildLowStockPdf(products, DateTime.UtcNow);
            await File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);

            var reportUrl = $"{publicBaseUrl}/reports/{fileName}";
            _logger.LogInformation("Low-stock PDF report generated: {ReportUrl}", reportUrl);

            return reportUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate the low-stock PDF report.");
            return null;
        }
    }

    /// <summary>
    /// Builds a minimal, valid, multi-page PDF document from scratch as raw bytes, using only the
    /// standard "Helvetica" font (no embedding required). Good enough for a plain text/tabular
    /// report; not a general-purpose PDF engine.
    /// </summary>
    private static byte[] BuildLowStockPdf(IReadOnlyList<(string Name, int StockQuantity)> products, DateTime generatedAtUtc)
    {
        const int pageWidth = 595;   // A4 width in points
        const int pageHeight = 842;  // A4 height in points
        const int marginLeft = 40;
        const int topY = 800;
        const int lineHeight = 16;
        const int linesPerPage = 45;

        var lines = new List<string>
        {
            "Low Stock Report",
            $"Generated: {generatedAtUtc:yyyy-MM-dd HH:mm} UTC",
            string.Empty,
            $"{"Product",-45}{"Stock Qty"}",
            new string('-', 60)
        };

        foreach (var product in products)
        {
            var name = product.Name.Length > 42 ? product.Name[..42] : product.Name;
            lines.Add($"{name,-45}{product.StockQuantity}");
        }

        var pages = new List<List<string>>();
        for (var i = 0; i < lines.Count; i += linesPerPage)
        {
            pages.Add(lines.Skip(i).Take(linesPerPage).ToList());
        }

        if (pages.Count == 0)
        {
            pages.Add(new List<string> { "Low Stock Report" });
        }

        var buffer = new List<byte>();
        var offsets = new List<int>();

        void WriteRaw(string text) => buffer.AddRange(Encoding.Latin1.GetBytes(text));

        void StartObject(int number)
        {
            offsets.Add(buffer.Count);
            WriteRaw($"{number} 0 obj\n");
        }

        void EndObject() => WriteRaw("endobj\n");

        WriteRaw("%PDF-1.4\n%\u00e2\u00e3\u00cf\u00d3\n");

        const int fontObjNum = 3;
        const int firstPageObjNum = 4;
        var totalPages = pages.Count;

        // 1: Catalog
        StartObject(1);
        WriteRaw("<< /Type /Catalog /Pages 2 0 R >>\n");
        EndObject();

        // 2: Pages
        StartObject(2);
        var kids = string.Join(" ", Enumerable.Range(0, totalPages).Select(k => $"{firstPageObjNum + (k * 2)} 0 R"));
        WriteRaw($"<< /Type /Pages /Kids [{kids}] /Count {totalPages} >>\n");
        EndObject();

        // 3: Font (standard Helvetica - no embedding needed)
        StartObject(fontObjNum);
        WriteRaw("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\n");
        EndObject();

        for (var k = 0; k < totalPages; k++)
        {
            var pageObjNum = firstPageObjNum + (k * 2);
            var contentObjNum = pageObjNum + 1;

            var content = new StringBuilder();
            content.Append("BT\n/F1 11 Tf\n");
            content.Append($"{marginLeft} {topY} Td\n{lineHeight} TL\n");

            var first = true;
            foreach (var line in pages[k])
            {
                var escaped = EscapePdfText(line);
                content.Append(first ? $"({escaped}) Tj\n" : $"T*\n({escaped}) Tj\n");
                first = false;
            }

            content.Append("ET");

            var contentBytes = Encoding.Latin1.GetBytes(content.ToString());

            StartObject(pageObjNum);
            WriteRaw(
                $"<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 {fontObjNum} 0 R >> >> " +
                $"/MediaBox [0 0 {pageWidth} {pageHeight}] /Contents {contentObjNum} 0 R >>\n");
            EndObject();

            StartObject(contentObjNum);
            WriteRaw($"<< /Length {contentBytes.Length} >>\nstream\n");
            buffer.AddRange(contentBytes);
            WriteRaw("\nendstream\n");
            EndObject();
        }

        var xrefOffset = buffer.Count;
        var objectCount = offsets.Count;

        WriteRaw($"xref\n0 {objectCount + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            WriteRaw($"{offset:D10} 00000 n \n");
        }

        WriteRaw($"trailer\n<< /Size {objectCount + 1} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");

        return buffer.ToArray();
    }

    private static string EscapePdfText(string text) =>
        text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string BuildTruncatedProductSummary(IEnumerable<string> productEntries, int maxLength)
    {
        var entries = productEntries.ToList();
        var summary = string.Join(", ", entries);

        if (summary.Length <= maxLength)
        {
            return summary;
        }

        // Add entries one at a time until adding the next one (plus a "+N more" suffix) would exceed the limit.
        var builder = new System.Text.StringBuilder();
        var includedCount = 0;

        foreach (var entry in entries)
        {
            var candidate = builder.Length == 0 ? entry : builder + ", " + entry;
            var remaining = entries.Count - (includedCount + 1);
            var suffix = remaining > 0 ? $" +{remaining} more" : string.Empty;

            if (candidate.Length + suffix.Length > maxLength)
            {
                break;
            }

            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(entry);
            includedCount++;
        }

        var remainingCount = entries.Count - includedCount;
        if (remainingCount > 0)
        {
            builder.Append($" +{remainingCount} more");
        }

        var result = builder.ToString();

        // Safety net: hard-cut in the unlikely case a single entry itself is longer than maxLength.
        return result.Length > maxLength ? result[..maxLength] : result;
    }
}