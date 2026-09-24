using FluentValidation;

namespace ECommerce.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private const string SlugPattern = @"^[\p{L}\p{N}]+(?:-[\p{L}\p{N}]+)*$";

    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Request.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Request.Slug)
            .Matches(SlugPattern)
                .WithMessage("Slug must be lowercase alphanumeric with hyphens only (e.g. 'iphone-15').")
            .MaximumLength(200)
                .WithMessage("Slug must not exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Slug));

        RuleFor(x => x.Request.Sku)
            .NotEmpty().WithMessage("SKU is required.")
            .MaximumLength(50).WithMessage("SKU must not exceed 50 characters.");

        RuleFor(x => x.Request.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.Request.QuantityInStock)
            .GreaterThanOrEqualTo(0).WithMessage("QuantityInStock must be a non-negative integer.");
    }
}
