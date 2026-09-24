using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

/// <summary>
/// Validates <see cref="CreateProductRequest"/> before it reaches the service layer.
/// Business rules (category active check, slug uniqueness) are enforced in ProductService.
/// </summary>
public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    private const string SlugPattern = @"^[\p{L}\p{N}]+(?:-[\p{L}\p{N}]+)*$";

    public CreateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Slug)
            .Matches(SlugPattern)
                .WithMessage("Slug must be lowercase alphanumeric with hyphens only (e.g. 'iphone-15').")
            .MaximumLength(200)
                .WithMessage("Slug must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters.");



        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.QuantityInStock)
            .GreaterThanOrEqualTo(0).WithMessage("QuantityInStock must be a non-negative integer.");
    }
}
