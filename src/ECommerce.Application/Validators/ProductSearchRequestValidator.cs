using ECommerce.Application.Contracts;
using FluentValidation;

namespace ECommerce.Application.Validators;

/// <summary>
/// Validates <see cref="ProductSearchRequest"/> query parameters.
/// Invalid sort values default gracefully in the service; only price range inversion is rejected.
/// </summary>
public sealed class ProductSearchRequestValidator : AbstractValidator<ProductSearchRequest>
{
    private static readonly HashSet<string> AllowedSortValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "Price", "Newest", "Oldest"
    };

    public ProductSearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("PageSize must not exceed 100.");

        RuleFor(x => x)
            .Must(x => x.PriceFrom is null || x.PriceTo is null || x.PriceFrom <= x.PriceTo)
                .WithMessage("PriceFrom must be less than or equal to PriceTo.")
            .When(x => x.PriceFrom.HasValue && x.PriceTo.HasValue);

        RuleFor(x => x.PriceFrom)
            .GreaterThanOrEqualTo(0).WithMessage("PriceFrom must not be negative.")
            .When(x => x.PriceFrom.HasValue);

        RuleFor(x => x.PriceTo)
            .GreaterThanOrEqualTo(0).WithMessage("PriceTo must not be negative.")
            .When(x => x.PriceTo.HasValue);
    }
}
