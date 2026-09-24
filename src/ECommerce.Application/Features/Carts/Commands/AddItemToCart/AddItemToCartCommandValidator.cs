using FluentValidation;

namespace ECommerce.Application.Features.Carts.Commands.AddItemToCart;

public sealed class AddItemToCartCommandValidator : AbstractValidator<AddItemToCartCommand>
{
    public AddItemToCartCommandValidator()
    {
        RuleFor(x => x.Request.ProductId).NotEmpty();
        RuleFor(x => x.Request.Quantity).GreaterThan(0);
    }
}
