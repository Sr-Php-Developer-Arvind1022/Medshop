namespace Medshop.Modules.Identity.Application.DTOs.Response;

public class RegisterResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? ProfileImage { get; set; }
    public bool IsActive { get; set; }
}
