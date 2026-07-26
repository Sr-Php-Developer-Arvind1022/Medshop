namespace Medshop.Modules.Identity.Application.DTOs.Response;

public class ForgotPasswordResponse
{
    public string Email { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
    public DateTime ResetTokenExpiresAtUtc { get; set; }
}