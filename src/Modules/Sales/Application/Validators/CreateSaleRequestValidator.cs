using FluentValidation;
using Medshop.Modules.Sales.Application.DTOs.Request;
using System.Text.Json;

namespace Medshop.Modules.Sales.Application.Validators;

public class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    private static readonly string[] AllowedPaymentModes =
    [
        "Cash", "Card", "UPI", "Bank Transfer", "Credit"
    ];

    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.Customer).NotNull();

        RuleFor(x => x.Customer.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters.");

        RuleFor(x => x.Customer.Mobile)
            .NotEmpty().WithMessage("Customer mobile is required.")
            .MaximumLength(20).WithMessage("Customer mobile cannot exceed 20 characters.");

        RuleFor(x => x.Customer.Address)
            .MaximumLength(500).WithMessage("Customer address cannot exceed 500 characters.");

        RuleFor(x => x.Discount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount cannot be negative.");

        RuleFor(x => x.Tax)
            .GreaterThanOrEqualTo(0).WithMessage("Tax cannot be negative.");

        RuleFor(x => x.PaymentMode)
            .NotEmpty().WithMessage("Payment mode is required.")
            .Must(mode => AllowedPaymentModes.Contains(mode)).WithMessage("Invalid payment mode.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one sale item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i)
                .Must(HaveValidProductReference)
                .WithMessage("Product reference is required. Use numeric product_fk (product_pk) or product GUID.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        });
    }

    private static bool HaveValidProductReference(CreateSaleItemRequest item)
    {
        var token = item.ProductFk.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? item.ProductFkCamel
            : item.ProductFk;

        if (token.ValueKind == JsonValueKind.Number)
        {
            return token.TryGetInt64(out var productPk) && productPk > 0;
        }

        if (token.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var value = token.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return long.TryParse(value, out var productPkFromString) && productPkFromString > 0
            || Guid.TryParse(value, out _);
    }
}
