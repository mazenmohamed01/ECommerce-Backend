using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

public sealed class UpdateCartItemRequestValidator : AbstractValidator<UpdateCartItemRequest>
{
    public UpdateCartItemRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .InclusiveBetween(0, 100).WithMessage("Quantity must be between 0 and 100.");
    }
}
