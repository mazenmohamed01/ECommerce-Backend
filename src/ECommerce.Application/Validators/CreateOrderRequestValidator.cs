using ECommerce.Application.Contracts;
using FluentValidation;
using System.Text.RegularExpressions;

namespace ECommerce.Application.Validators;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(100).WithMessage("Customer name cannot exceed 100 characters.");

        RuleFor(x => x.CustomerPhone)
            .NotEmpty().WithMessage("Customer phone is required.")
            .Matches(@"^(05)(5|0|3|6|4|9|1|8|7)([0-9]{7})$")
            .WithMessage("Phone number must be a valid Saudi mobile number (e.g. 05xxxxxxxx).");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Street address is required.")
            .MaximumLength(300).WithMessage("Street address cannot exceed 300 characters.");

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("District is required.")
            .MaximumLength(100).WithMessage("District cannot exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State/Region is required.")
            .MaximumLength(100).WithMessage("State/Region cannot exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");

        RuleFor(x => x.City)
            .IsInEnum().WithMessage("Invalid Saudi city selected.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Payment method must be Cash or Online.");
    }
}
