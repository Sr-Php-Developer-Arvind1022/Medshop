using FluentValidation;
using Medshop.Modules.Identity.Application.DTOs.Request;

namespace Medshop.Modules.Identity.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Mobile)
            .NotEmpty().WithMessage("Mobile is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("(?=.*[A-Z])").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("(?=.*[a-z])").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("(?=.*\\d)").WithMessage("Password must contain at least one number.")
            .Matches("(?=.*[^A-Za-z0-9])").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Confirm password must match password.");

        RuleFor(x => x.ProfileImage)
            .Must(BeValidImage)
            .When(x => x.ProfileImage is not null)
            .WithMessage("Only jpg, jpeg, and png files are allowed and maximum size is 2MB.");
    }

    private static bool BeValidImage(IFormFile? image)
    {
        if (image is null) return true;
        if (image.Length == 0 || image.Length > 2 * 1024 * 1024) return false;
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png";
    }
}
