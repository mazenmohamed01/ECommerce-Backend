using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class AddCartItemRequestValidator : AbstractValidator<AddCartItemRequest>
{
    public AddCartItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 100).WithMessage("Quantity must be between 1 and 100.");
    }
}
