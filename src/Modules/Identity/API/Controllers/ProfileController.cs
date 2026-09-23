using System.Security.Claims;
using System.Text.Json;
using Medshop.BuildingBlocks.Common;
using Medshop.Modules.Identity.Domain.Entities;
using Medshop.Modules.Identity.Infrastructure.JWT;
using Medshop.Modules.Identity.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medshop.Modules.Identity.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly MedshopDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public ProfileController(MedshopDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProfileSettingsDto>>> GetProfileDetails(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Ok(ApiResponse<ProfileSettingsDto>.SuccessResult(new ProfileSettingsDto(), "Profile details fetched successfully"));
        }

        var profile = MapUserToProfile(user);
        return Ok(ApiResponse<ProfileSettingsDto>.SuccessResult(profile, "Profile details fetched successfully"));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<ProfileSettingsDto>>> UpdateProfile([FromBody] UpdateProfileSettingsRequest request, CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(ApiResponse<ProfileSettingsDto>.FailureResult("User not found."));
        }

        if (request.BasicDetails is not null)
        {
            user.BusinessName = !string.IsNullOrWhiteSpace(request.BasicDetails.BusinessName)
                ? request.BasicDetails.BusinessName
                : user.BusinessName;

            user.OwnerName = !string.IsNullOrWhiteSpace(request.BasicDetails.OwnerName)
                ? request.BasicDetails.OwnerName
                : user.OwnerName;

            user.Mobile = !string.IsNullOrWhiteSpace(request.BasicDetails.Phone)
                ? request.BasicDetails.Phone
                : user.Mobile;

            user.Email = !string.IsNullOrWhiteSpace(request.BasicDetails.Email)
                ? request.BasicDetails.Email
                : user.Email;

            user.Address = !string.IsNullOrWhiteSpace(request.BasicDetails.Address)
                ? request.BasicDetails.Address
                : user.Address;

            user.City = !string.IsNullOrWhiteSpace(request.BasicDetails.City)
                ? request.BasicDetails.City
                : user.City;

            user.State = !string.IsNullOrWhiteSpace(request.BasicDetails.State)
                ? request.BasicDetails.State
                : user.State;
        }

        if (request.WhatsApp is not null)
        {
            user.WhatsAppBaseUrl = !string.IsNullOrWhiteSpace(request.WhatsApp.BaseUrl)
                ? request.WhatsApp.BaseUrl
                : user.WhatsAppBaseUrl;

            user.WhatsAppApiKey = !string.IsNullOrWhiteSpace(request.WhatsApp.ApiKey)
                ? request.WhatsApp.ApiKey
                : user.WhatsAppApiKey;

            if (request.WhatsApp.Templates is not null)
            {
                var templates = request.WhatsApp.Templates
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                user.WhatsAppTemplatesJson = JsonSerializer.Serialize(templates, JsonOptions);
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var updatedProfile = MapUserToProfile(user);
        return Ok(ApiResponse<ProfileSettingsDto>.SuccessResult(updatedProfile, "Profile updated successfully"));
    }

    [HttpPut("whatsapp/templates")]
    public async Task<ActionResult<ApiResponse<List<string>>>> UpdateWhatsAppTemplates([FromBody] UpdateWhatsAppTemplatesRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.Templates is null || request.Templates.Count == 0)
        {
            return BadRequest(ApiResponse<List<string>>.FailureResult("At least one WhatsApp template name is required."));
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized(ApiResponse<List<string>>.FailureResult("User not found."));
        }

        var templates = request.Templates
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        user.WhatsAppTemplatesJson = JsonSerializer.Serialize(templates, JsonOptions);
        user.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<List<string>>.SuccessResult(templates, "WhatsApp templates updated successfully"));
    }

    private async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var loginIdValue = User.FindFirstValue(JwtClaimTypes.LoginId);
        if (Guid.TryParse(loginIdValue, out var loginId))
        {
            var userById = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == loginId, cancellationToken);
            if (userById is not null)
            {
                return userById;
            }
        }

        var email = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(email))
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
        }

        return null;
    }

    private static ProfileSettingsDto MapUserToProfile(User user)
    {
        var templates = DeserializeTemplates(user.WhatsAppTemplatesJson);

        return new ProfileSettingsDto
        {
            BasicDetails = new BasicProfileDetailsDto
            {
                BusinessName = user.BusinessName,
                OwnerName = user.OwnerName,
                Phone = user.Mobile,
                Email = user.Email,
                Address = user.Address,
                City = user.City,
                State = user.State
            },
            WhatsApp = new WhatsAppSettingsDto
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
