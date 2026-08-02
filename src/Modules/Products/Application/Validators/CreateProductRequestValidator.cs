using FluentValidation;
using Medshop.Modules.Products.Application.DTOs.Request;

namespace Medshop.Modules.Products.Application.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0).WithMessage("Purchase price must be greater than zero.");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0).WithMessage("Selling price must be greater than zero.")
            .GreaterThanOrEqualTo(x => x.PurchasePrice).WithMessage("Selling price cannot be less than purchase price.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.ProductImage)
            .Must(BeValidImage)
            .When(x => x.ProductImage is not null)
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