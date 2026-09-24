using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class AdminOrderSearchRequestValidator : AbstractValidator<AdminOrderSearchRequest>
{
    public AdminOrderSearchRequestValidator()
    {
        RuleFor(x => x.PageSize)
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100.")
            .GreaterThan(0).WithMessage("Page size must be greater than 0.");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than zero.");

        RuleFor(x => x)
            .Must(x => x.DateFrom <= x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("DateFrom must be before or equal to DateTo.");

        RuleFor(x => x.Status)
            .IsInEnum().When(x => x.Status.HasValue).WithMessage("Invalid status filter.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().When(x => x.PaymentMethod.HasValue).WithMessage("Invalid payment method filter.");
    }
}
