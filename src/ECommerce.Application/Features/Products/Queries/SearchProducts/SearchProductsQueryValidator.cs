using FluentValidation;

namespace ECommerce.Application.Features.Products.Queries.SearchProducts;

public sealed class SearchProductsQueryValidator : AbstractValidator<SearchProductsQuery>
{
    private static readonly HashSet<string> AllowedSortValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "Price", "Newest", "Oldest"
    };

    public SearchProductsQueryValidator()
    {
        RuleFor(x => x.Request.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.Request.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("PageSize must be at least 1.")
            .LessThanOrEqualTo(100).WithMessage("PageSize must not exceed 100.");

        RuleFor(x => x.Request)
            .Must(x => x.PriceFrom is null || x.PriceTo is null || x.PriceFrom <= x.PriceTo)
                .WithMessage("PriceFrom must be less than or equal to PriceTo.")
            .When(x => x.Request.PriceFrom.HasValue && x.Request.PriceTo.HasValue);

        RuleFor(x => x.Request.PriceFrom)
            .GreaterThanOrEqualTo(0).WithMessage("PriceFrom must not be negative.")
            .When(x => x.Request.PriceFrom.HasValue);

        RuleFor(x => x.Request.PriceTo)
            .GreaterThanOrEqualTo(0).WithMessage("PriceTo must not be negative.")
            .When(x => x.Request.PriceTo.HasValue);
    }
}
