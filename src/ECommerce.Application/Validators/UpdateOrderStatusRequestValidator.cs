using ECommerce.Application.Contracts;
using ECommerce.Domain.Enums;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid order status.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.NewStatus == OrderStatus.Cancelled)
            .WithMessage("Reason is required when cancelling an order.");
            
        RuleFor(x => x.Reason)
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
