using FluentValidation;
using Medshop.Modules.Categories.Application.DTOs.Request;

namespace Medshop.Modules.Categories.Application.Validators;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(200).WithMessage("Category name cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.CategoryImage)
            .Must(BeValidImage)
            .When(x => x.CategoryImage is not null)
            .WithMessage("Only jpg, jpeg, and png files are allowed and maximum size is 5MB.");
    }

    private static bool BeValidImage(IFormFile? image)
    {
        if (image is null)
        {
            return true;
        }

        if (image.Length == 0 || image.Length > 5 * 1024 * 1024)
        {
            return false;
        }

        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png";
    }
}
