using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
        var profileTemplates = DeserializeTemplates(profileUser.WhatsAppTemplatesJson);
        var templateCode = profileTemplates.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
            ?? _configuration["LowStockAlert:TemplateCode"]
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

        var ownerName = profileUser.OwnerName;
        var productSummary = string.Join(", ", products.Select(p => $"{p.Name} ({p.StockQuantity})"));

        var payload = new Dictionary<string, object?>
        {
            ["recipient_phone"] = recipientPhone,
            ["template_code"] = templateCode,
            ["variables"] = new Dictionary<string, object>
            {
                ["name"] = !string.IsNullOrWhiteSpace(ownerName) ? ownerName : "Customer",
                ["products"] = productSummary,
                ["count"] = products.Count.ToString()
            }
        };

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

    private static List<string> DeserializeTemplates(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            var templates = JsonSerializer.Deserialize<List<string>>(json);
            return templates ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
