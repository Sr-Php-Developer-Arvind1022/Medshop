using System.Text.Json;
using Medshop.BuildingBlocks.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medshop.Modules.Identity.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly string _settingsFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ProfileController(IWebHostEnvironment environment)
    {
        _environment = environment;

        var dataDirectory = Path.Combine(_environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);

        _settingsFilePath = Path.Combine(dataDirectory, "profile-settings.json");
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProfileSettingsDto>>> GetProfileDetails(CancellationToken cancellationToken)
    {
        var profile = await LoadSettingsAsync(cancellationToken);
        return Ok(ApiResponse<ProfileSettingsDto>.SuccessResult(profile, "Profile details fetched successfully"));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<ProfileSettingsDto>>> UpdateProfile([FromBody] UpdateProfileSettingsRequest request, CancellationToken cancellationToken)
    {
        var profile = await LoadSettingsAsync(cancellationToken);

        if (request.BasicDetails is not null)
        {
            profile.BasicDetails ??= new BasicProfileDetailsDto();

            profile.BasicDetails.BusinessName = !string.IsNullOrWhiteSpace(request.BasicDetails.BusinessName)
                ? request.BasicDetails.BusinessName
                : profile.BasicDetails.BusinessName;

            profile.BasicDetails.OwnerName = !string.IsNullOrWhiteSpace(request.BasicDetails.OwnerName)
                ? request.BasicDetails.OwnerName
                : profile.BasicDetails.OwnerName;

            profile.BasicDetails.Phone = !string.IsNullOrWhiteSpace(request.BasicDetails.Phone)
                ? request.BasicDetails.Phone
                : profile.BasicDetails.Phone;

            profile.BasicDetails.Email = !string.IsNullOrWhiteSpace(request.BasicDetails.Email)
                ? request.BasicDetails.Email
                : profile.BasicDetails.Email;

            profile.BasicDetails.Address = !string.IsNullOrWhiteSpace(request.BasicDetails.Address)
                ? request.BasicDetails.Address
                : profile.BasicDetails.Address;

            profile.BasicDetails.City = !string.IsNullOrWhiteSpace(request.BasicDetails.City)
                ? request.BasicDetails.City
                : profile.BasicDetails.City;

            profile.BasicDetails.State = !string.IsNullOrWhiteSpace(request.BasicDetails.State)
                ? request.BasicDetails.State
                : profile.BasicDetails.State;
        }

        if (request.WhatsApp is not null)
        {
            profile.WhatsApp ??= new WhatsAppSettingsDto();

            profile.WhatsApp.BaseUrl = !string.IsNullOrWhiteSpace(request.WhatsApp.BaseUrl)
                ? request.WhatsApp.BaseUrl
                : profile.WhatsApp.BaseUrl;

            profile.WhatsApp.ApiKey = !string.IsNullOrWhiteSpace(request.WhatsApp.ApiKey)
                ? request.WhatsApp.ApiKey
                : profile.WhatsApp.ApiKey;

            if (request.WhatsApp.Templates is not null)
            {
                profile.WhatsApp.Templates = request.WhatsApp.Templates
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        await SaveSettingsAsync(profile, cancellationToken);
        return Ok(ApiResponse<ProfileSettingsDto>.SuccessResult(profile, "Profile updated successfully"));
    }

    [HttpPut("whatsapp/templates")]
    public async Task<ActionResult<ApiResponse<List<string>>>> UpdateWhatsAppTemplates([FromBody] UpdateWhatsAppTemplatesRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.Templates is null || request.Templates.Count == 0)
        {
            return BadRequest(ApiResponse<List<string>>.FailureResult("At least one WhatsApp template name is required."));
        }

        var profile = await LoadSettingsAsync(cancellationToken);
        profile.WhatsApp ??= new WhatsAppSettingsDto();

        profile.WhatsApp.Templates = request.Templates
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        await SaveSettingsAsync(profile, cancellationToken);
        return Ok(ApiResponse<List<string>>.SuccessResult(profile.WhatsApp.Templates, "WhatsApp templates updated successfully"));
    }

    private async Task<ProfileSettingsDto> LoadSettingsAsync(CancellationToken cancellationToken)
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

    private async Task SaveSettingsAsync(ProfileSettingsDto profile, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        await System.IO.File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);
    }
}

public class ProfileSettingsDto
{
    public BasicProfileDetailsDto BasicDetails { get; set; } = new();
    public WhatsAppSettingsDto WhatsApp { get; set; } = new();
}

public class UpdateProfileSettingsRequest
{
    public BasicProfileDetailsDto? BasicDetails { get; set; }
    public WhatsAppSettingsDto? WhatsApp { get; set; }
}

public class UpdateWhatsAppTemplatesRequest
{
    public List<string> Templates { get; set; } = new();
}

public class BasicProfileDetailsDto
{
    public string? BusinessName { get; set; }
    public string? OwnerName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
}

public class WhatsAppSettingsDto
{
    public string? BaseUrl { get; set; } = "https://sahilmoney.in/WapHubBackend";
    public string? ApiKey { get; set; }
    public List<string> Templates { get; set; } = new();
}
