using FluentValidation;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusCommandValidator()
    {
        RuleFor(x => x.Request.NewStatus).IsInEnum().WithMessage("Invalid order status.");
        RuleFor(x => x.Request.Reason).MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
    }
}
