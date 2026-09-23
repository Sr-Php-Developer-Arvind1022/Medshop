using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Medshop.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.WhatsApp.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WhatsAppController> _logger;
    private readonly MedshopDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public WhatsAppController(
        IHttpClientFactory httpClientFactory,
        ILogger<WhatsAppController> logger,
        MedshopDbContext dbContext)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _dbContext = dbContext;
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

        var profile = await LoadProfileSettingsAsync(request.UserId, cancellationToken);
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

        // media_url is optional for now — left blank unless explicitly provided and valid.
        var mediaUrl = request.MediaUrl;

        if (!string.IsNullOrWhiteSpace(mediaUrl))
        {
            if (!Uri.TryCreate(mediaUrl, UriKind.Absolute, out var mediaUri) ||
                (!mediaUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !mediaUri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest(ApiResponse<object>.FailureResult("media_url must be a full public URL like https://example.com/invoice.pdf."));
            }
        }
        else
        {
            mediaUrl = null;
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


    private async Task<ProfileSettingsDto> LoadProfileSettingsAsync(Guid? userId, CancellationToken cancellationToken)
    {
        var currentUser = await GetCurrentUserAsync(userId, cancellationToken);
        if (currentUser is null)
        {
            return new ProfileSettingsDto();
        }

        return MapUserToProfile(currentUser);
    }

    private async Task<User?> GetCurrentUserAsync(Guid? explicitUserId, CancellationToken cancellationToken)
    {
        if (explicitUserId.HasValue)
        {
            var userById = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == explicitUserId.Value, cancellationToken);
            if (userById is not null)
            {
                return userById;
            }
        }

        var loginIdValue = User.FindFirstValue(JwtClaimTypes.LoginId);
        if (Guid.TryParse(loginIdValue, out var loginId))
        {
            var userByLoginId = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == loginId, cancellationToken);
            if (userByLoginId is not null)
            {
                return userByLoginId;
            }
        }

        var email = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(email))
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        return await _dbContext.Users.FirstOrDefaultAsync(u => !string.IsNullOrWhiteSpace(u.WhatsAppApiKey), cancellationToken);
    }

    private static ProfileSettingsDto MapUserToProfile(User user)
    {
        var templates = DeserializeTemplates(user.WhatsAppTemplatesJson);

        return new ProfileSettingsDto
        {
            WhatsApp = new WhatsAppProfileSettingsDto
            {
                BaseUrl = string.IsNullOrWhiteSpace(user.WhatsAppBaseUrl) ? "https://sahilmoney.in/WapHubBackend" : user.WhatsAppBaseUrl,
                ApiKey = user.WhatsAppApiKey,
                Templates = templates
            }
        };
    }

    private static List<string> DeserializeTemplates(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            var templates = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            return templates ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

public class SendWhatsAppTemplateRequest
{
    [JsonPropertyName("user_id")]
    public Guid? UserId { get; set; }

    [JsonPropertyName("template_name")]
    public string TemplateName { get; set; } = string.Empty;

    [JsonPropertyName("recipient_phone")]
    public string RecipientPhone { get; set; } = string.Empty;

    [JsonPropertyName("variables")]
    public Dictionary<string, object>? Variables { get; set; } = new();

    [JsonPropertyName("media_url")]
    public string? MediaUrl { get; set; } = string.Empty;
}

public class ProfileSettingsDto
{
    public WhatsAppProfileSettingsDto? WhatsApp { get; set; } = new();
}

public class WhatsAppProfileSettingsDto
{
    public string? BaseUrl { get; set; } = "https://sahilmoney.in/WapHubBackend";
    public string? ApiKey { get; set; }
    public List<string> Templates { get; set; } = new();
}