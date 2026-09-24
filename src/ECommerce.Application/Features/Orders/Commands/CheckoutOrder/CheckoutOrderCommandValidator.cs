using FluentValidation;

namespace ECommerce.Application.Features.Orders.Commands.CheckoutOrder;

public sealed class CheckoutOrderCommandValidator : AbstractValidator<CheckoutOrderCommand>
{
    public CheckoutOrderCommandValidator()
    {
        RuleFor(x => x.Request.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.Request.CustomerPhone)
            .NotEmpty().WithMessage("Customer phone is required.")
            .Matches(@"^(05)(5|0|3|6|4|9|1|8|7)([0-9]{7})$")
            .WithMessage("Phone number must be a valid Saudi mobile number (e.g. 05xxxxxxxx).");

        RuleFor(x => x.Request.Street)
            .NotEmpty().WithMessage("Street address is required.")
            .MaximumLength(300).WithMessage("Street address cannot exceed 300 characters.");

        RuleFor(x => x.Request.District)
            .NotEmpty().WithMessage("District is required.")
            .MaximumLength(100).WithMessage("District cannot exceed 100 characters.");

        RuleFor(x => x.Request.State)
            .NotEmpty().WithMessage("State/Region is required.")
            .MaximumLength(100).WithMessage("State/Region cannot exceed 100 characters.");

        RuleFor(x => x.Request.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");

        RuleFor(x => x.Request.PaymentMethod)
            .IsInEnum().WithMessage("Payment method must be Cash or Online.");
    }
}
