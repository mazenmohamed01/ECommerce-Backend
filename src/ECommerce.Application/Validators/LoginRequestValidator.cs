using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

/// <summary>
/// Validates <see cref="LoginRequest"/> before it reaches the service layer.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
