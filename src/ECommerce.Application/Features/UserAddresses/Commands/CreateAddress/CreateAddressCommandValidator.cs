using FluentValidation;

namespace ECommerce.Application.Features.UserAddresses.Commands.CreateAddress;

public class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
        RuleFor(x => x.Request.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(100);
        RuleFor(x => x.Request.Street).NotEmpty().WithMessage("Street is required.").MaximumLength(255);
        RuleFor(x => x.Request.District).NotEmpty().WithMessage("District is required.").MaximumLength(100);
        RuleFor(x => x.Request.City).NotEmpty().WithMessage("City is required.").MaximumLength(100);
        RuleFor(x => x.Request.State).NotEmpty().WithMessage("State is required.").MaximumLength(100);
        RuleFor(x => x.Request.PostalCode).NotEmpty().WithMessage("Postal Code is required.").MaximumLength(20);
        RuleFor(x => x.Request.Country).NotEmpty().WithMessage("Country is required.").MaximumLength(100);
    }
}
