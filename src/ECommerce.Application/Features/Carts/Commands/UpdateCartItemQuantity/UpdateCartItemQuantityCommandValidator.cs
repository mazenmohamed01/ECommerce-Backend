using FluentValidation;

namespace ECommerce.Application.Features.Carts.Commands.UpdateCartItemQuantity;

public sealed class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
{
    public UpdateCartItemQuantityCommandValidator()
    {
        RuleFor(x => x.Request.Quantity).GreaterThanOrEqualTo(0);
    }
}
