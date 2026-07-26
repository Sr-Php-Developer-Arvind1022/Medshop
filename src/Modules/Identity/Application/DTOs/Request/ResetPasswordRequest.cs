namespace Medshop.Modules.Identity.Application.DTOs.Request;

public class ResetPasswordRequest
{
    public string ResetToken { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}