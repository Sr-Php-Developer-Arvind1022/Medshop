using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Medshop.BuildingBlocks.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace Medshop.Modules.WhatsApp.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WhatsAppController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _settingsFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public WhatsAppController(
        IHttpClientFactory httpClientFactory,
        ILogger<WhatsAppController> logger,
        IWebHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _environment = environment;

        var dataDirectory = Path.Combine(_environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);

        _settingsFilePath = Path.Combine(dataDirectory, "profile-settings.json");
    }

    [HttpPost("send-template")]
    public async Task<ActionResult<ApiResponse<object>>> SendTemplate(
        [FromBody] SendWhatsAppTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientPhone))
        {
            return BadRequest(ApiResponse<object>.FailureResult("Recipient phone is required."));
        }

        var profile = await LoadProfileSettingsAsync(cancellationToken);
        var apiKey = profile.WhatsApp?.ApiKey;

        var templateCode = profile.WhatsApp?.Templates?
            .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t) &&
                string.Equals(t, request.TemplateName, StringComparison.OrdinalIgnoreCase))
            ?? profile.WhatsApp?.Templates?
                .FirstOrDefault(t => !string.IsNullOrWhiteSpace(t));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest(ApiResponse<object>.FailureResult("WhatsApp API key is not configured in the saved profile."));
        }

        if (string.IsNullOrWhiteSpace(templateCode))
        {
            return BadRequest(ApiResponse<object>.FailureResult("No WhatsApp template is configured in the saved profile."));
        }

        var client = _httpClientFactory.CreateClient("WapHub");
        var savedBaseUrl = profile.WhatsApp?.BaseUrl;
        if (!string.IsNullOrWhiteSpace(savedBaseUrl))
        {
            client.BaseAddress = new Uri(savedBaseUrl.TrimEnd('/') + "/");
        }

        var mediaUrl = request.MediaUrl;
        string? generatedFileName = null;

        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            var generatedPdf = await CreateInvoicePdfAsync(request, profile, cancellationToken);
            generatedFileName = Path.GetFileName(generatedPdf.FilePath);
            mediaUrl = ResolvePublicMediaUrl(profile, generatedFileName);

            if (string.IsNullOrWhiteSpace(mediaUrl))
            {
                return BadRequest(ApiResponse<object>.FailureResult("A public media_url is required for WhatsApp PDF attachments. Use the full public base URL, not localhost."));
            }
        }

        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (request.Variables is not null)
        {
            foreach (var item in request.Variables)
            {
                variables[item.Key] = item.Value ?? string.Empty;
            }
        }

        if (!variables.ContainsKey("name"))
        {
            if (request.Variables is not null && request.Variables.TryGetValue("customer_name", out var customerNameValue) && customerNameValue is not null)
            {
                variables["name"] = customerNameValue.ToString() ?? "Customer";
            }
            else
            {
                variables["name"] = "Customer";
            }
        }

        if (!variables.ContainsKey("items"))
        {
            if (request.Variables is not null && request.Variables.TryGetValue("item", out var itemValue) && itemValue is not null)
            {
                variables["items"] = itemValue.ToString() ?? "Invoice";
            }
            else if (request.Variables is not null && request.Variables.TryGetValue("invoice_items", out var invoiceItemsValue) && invoiceItemsValue is not null)
            {
                variables["items"] = invoiceItemsValue.ToString() ?? "Invoice";
            }
            else
            {
                variables["items"] = "Invoice";
            }
        }

        if (!variables.ContainsKey("date"))
        {
            variables["date"] = DateTime.UtcNow.ToString("dd-MM-yyyy");
        }

        var payload = new Dictionary<string, object?>
        {
            ["recipient_phone"] = request.RecipientPhone,
            ["template_code"] = templateCode,
            ["variables"] = variables
        };

        if (!string.IsNullOrWhiteSpace(mediaUrl))
        {
            payload["media_url"] = mediaUrl;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/messages/send-template");
        httpRequest.Content = JsonContent.Create(payload);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Headers.TryAddWithoutValidation("X-API-KEY", apiKey);

        _logger.LogInformation(
            "Sending saved WhatsApp template. URL: {Url}, HasApiKey: {HasApiKey}, TemplateCode: {TemplateCode}, RecipientPhone: {RecipientPhone}, MediaUrl: {MediaUrl}",
            "api/v1/messages/send-template",
            !string.IsNullOrWhiteSpace(apiKey),
            templateCode,
            request.RecipientPhone,
            mediaUrl);

        var response = await client.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        _logger.LogWarning(
            "WhatsApp upstream response. StatusCode: {StatusCode}, Body: {ResponseBody}",
            (int)response.StatusCode,
            responseBody);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode,
                ApiResponse<object>.FailureResult(
                    "WhatsApp template request failed.",
                    new Dictionary<string, string[]>
                    {
                        ["external_api"] = [responseBody]
                    }));
        }

        return Ok(ApiResponse<object>.SuccessResult(new { message = responseBody, media_url = mediaUrl }, "WhatsApp template sent successfully"));
    }

    private async Task<(string MediaUrl, string FilePath)> CreateInvoicePdfAsync(SendWhatsAppTemplateRequest request, ProfileSettingsDto profile, CancellationToken cancellationToken)
    {
        var reportDirectory = Path.Combine(_environment.ContentRootPath, "Report");
        Directory.CreateDirectory(reportDirectory);

        var invoiceNumber = string.IsNullOrWhiteSpace(request.TemplateName)
            ? $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : request.TemplateName;

        var safeTitle = string.Concat(invoiceNumber.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'));
        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{(string.IsNullOrWhiteSpace(safeTitle) ? "invoice" : safeTitle)}.pdf";
        var filePath = Path.Combine(reportDirectory, fileName);

        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var titleFont = new XFont("Arial", 18, XFontStyle.Bold);
        var regularFont = new XFont("Arial", 11, XFontStyle.Regular);
        var y = 40d;
        gfx.DrawString("Medshop Invoice", titleFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 25), XStringFormats.TopLeft);
        y += 30;

        var lines = new List<string>
        {
            $"Template: {request.TemplateName}",
            $"Recipient: {request.RecipientPhone}",
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        };

        if (request.Variables is not null)
        {
            foreach (var pair in request.Variables)
            {
                var value = pair.Value switch
                {
                    null => "N/A",
                    string s => s,
                    _ => pair.Value.ToString()
                };

                lines.Add($"{pair.Key}: {value}");
            }
        }

        foreach (var line in lines)
        {
            gfx.DrawString(line, regularFont, XBrushes.Black, new XRect(40, y, page.Width - 80, 20), XStringFormats.TopLeft);
            y += 18;
        }

        document.Save(filePath);
        document.Close();

        var mediaUrl = ResolvePublicMediaUrl(profile, fileName);
        await Task.CompletedTask;
        return (mediaUrl ?? string.Empty, filePath);
    }

    private string? ResolvePublicMediaUrl(ProfileSettingsDto? profile, string fileName)
    {
        var configuredBase = profile?.WhatsApp?.PublicMediaBaseUrl;
        if (!string.IsNullOrWhiteSpace(configuredBase))
        {
            if (configuredBase.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
                configuredBase.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var trimmedBase = configuredBase.TrimEnd('/');
            if (trimmedBase.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return trimmedBase;
            }

            var filePath = Path.GetFileName(fileName);
            return $"{trimmedBase}/{filePath}";
        }

        return null;
    }

    private async Task<ProfileSettingsDto> LoadProfileSettingsAsync(CancellationToken cancellationToken)
    {
        if (!System.IO.File.Exists(_settingsFilePath))
        {
            return new ProfileSettingsDto();
        }

        var json = await System.IO.File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ProfileSettingsDto();
        }

        try
        {
            var profile = JsonSerializer.Deserialize<ProfileSettingsDto>(json, JsonOptions);
            return profile ?? new ProfileSettingsDto();
        }
        catch
        {
            return new ProfileSettingsDto();
        }
    }
}

public class SendWhatsAppTemplateRequest
{
    [JsonPropertyName("template_name")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("recipient_phone")]
    public string RecipientPhone { get; set; } = string.Empty;

    [JsonPropertyName("variables")]
    public Dictionary<string, object>? Variables { get; set; } = new();

    [JsonPropertyName("media_url")]
    public string? MediaUrl { get; set; }
}

public class ProfileSettingsDto
{
    public WhatsAppProfileSettingsDto? WhatsApp { get; set; } = new();
}

public class WhatsAppProfileSettingsDto
{
    public string? BaseUrl { get; set; } = "https://sahilmoney.in/WapHubBackend";
    public string? ApiKey { get; set; }
    public string? PublicMediaBaseUrl { get; set; }
    public List<string> Templates { get; set; } = new();
}
