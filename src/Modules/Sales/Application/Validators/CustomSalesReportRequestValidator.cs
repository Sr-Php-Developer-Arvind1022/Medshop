using FluentValidation;
using Medshop.Modules.Sales.Application.DTOs.Request;

namespace Medshop.Modules.Sales.Application.Validators;

public class CustomSalesReportRequestValidator : AbstractValidator<CustomSalesReportRequest>
{
    public CustomSalesReportRequestValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("From date is required.");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("To date is required.")
            .GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("To date must be greater than or equal to from date.");
    }
}
