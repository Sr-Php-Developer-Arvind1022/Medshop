using System.Net.Http.Headers;
using System.Net.Http.Json;
using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Identity.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
        var intervalMinutes = _configuration.GetValue<int?>("LowStockAlert:IntervalMinutes") ?? 5;
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
        var products = await dbContext.Products
            .Where(p => !p.IsDeleted && p.StockQuantity < threshold)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new { p.Name, p.StockQuantity })
            .ToListAsync(cancellationToken);

        var mediaUrl = await GenerateLowStockPdfReportAsync(
            products.Select(p => (p.Name, p.StockQuantity)).ToList(),
            cancellationToken);

        if (products.Count == 0)
        {
            _logger.LogInformation("No low-stock products found for alert check.");
            return;
        }

        var profileUser = await GetProfileUserAsync(cancellationToken);
        if (profileUser is null)
        {
            _logger.LogWarning("No profile user with WhatsApp configuration found. Low-stock alert skipped.");
            return;
        }

        var recipientPhone = profileUser.Mobile;
        var apiKey = profileUser.WhatsAppApiKey ?? _configuration["WhatsApp:ApiKey"];
        var templateCode = _configuration["LowStockAlert:TemplateCode"]
            ?? _configuration["WhatsApp:TemplateCode"]
            ?? "LOW_QUANTITY_PRODUCT";

        if (string.IsNullOrWhiteSpace(recipientPhone))
        {
            _logger.LogWarning("No recipient phone found in profile settings. Low-stock alert skipped.");
            return;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("WhatsApp API key missing. Low-stock alert skipped.");
            return;
        }

        var ownerName = !string.IsNullOrWhiteSpace(profileUser.OwnerName) ? profileUser.OwnerName : "Customer";
        var productSummary = BuildTruncatedProductSummary(products.Select(p => p.Name), maxLength: 120);

        // The WapHub template requires "name", "items", "amount" and "date". When a PDF report is
        // attached via media_url, the real product breakdown lives in the PDF, so we send dummy
        // placeholders for items/amount here instead of the actual summary/count.
        var variables = !string.IsNullOrWhiteSpace(mediaUrl)
            ? new Dictionary<string, object>
            {
                ["name"] = ownerName,
                ["items"] = "Test",
                ["amount"] = "0",
                ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
            }
            : new Dictionary<string, object>
            {
                ["name"] = ownerName,
                ["items"] = productSummary,
                ["amount"] = products.Count.ToString(),
                ["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
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
            _logger.LogWarning("Low-stock PDF report was not generated; sending alert without media_url.");
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
                    "Low-stock WhatsApp alert failed. StatusCode: {StatusCode}. Response: {ResponseBody}",
                    (int)response.StatusCode,
                    responseBody);
                return;
            }

            _logger.LogInformation(
                "Low-stock WhatsApp alert sent successfully to {RecipientPhone} for {ProductCount} product(s).",
                recipientPhone,
                products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending low-stock WhatsApp alert.");
        }
    }

    /// <summary>
    /// Generates a low-stock PDF report, saves it under wwwroot/reports (served as static files),
    /// and returns a publicly reachable URL built from App:PublicBaseUrl. Returns null on failure
    /// so the caller can still send the WhatsApp alert without an attachment.
    /// </summary>
    private async Task<string?> GenerateLowStockPdfReportAsync(
        IReadOnlyList<(string Name, int StockQuantity)> products,
        CancellationToken cancellationToken)
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

            var fileName = $"low-stock-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";
            var filePath = Path.Combine(reportsDirectory, fileName);

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Text("Low Stock Report").FontSize(18).Bold();

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Product").Bold();
                            header.Cell().AlignRight().Text("Stock Qty").Bold();
                        });

                        foreach (var product in products)
                        {
                            table.Cell().Text(product.Name);
                            table.Cell().AlignRight().Text(product.StockQuantity.ToString());
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Generated {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8);
                    });
                });
            }).GeneratePdf(filePath);

            // Run the blocking file write path off the sync GeneratePdf call above;
            // yield here to keep the async signature honest for callers awaiting this method.
            await Task.CompletedTask;

            return $"{publicBaseUrl}/reports/{fileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate the low-stock PDF report.");
            return null;
        }
    }

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

    private async Task<User?> GetProfileUserAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MedshopDbContext>();

        var user = await dbContext.Users
            .Where(u => !string.IsNullOrWhiteSpace(u.WhatsAppApiKey) || !string.IsNullOrWhiteSpace(u.WhatsAppTemplatesJson))
            .OrderByDescending(u => u.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
}