using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using System;
using System.Linq;

namespace ECommerce.Application.Features.Products.Queries.SearchProducts;

internal sealed class SearchProductsQueryHandler : IQueryHandler<SearchProductsQuery, Result<PagedResponse<ProductListItemResponse>>>
{
    private readonly IProductRepository _productRepository;

    public SearchProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<PagedResponse<ProductListItemResponse>>> Handle(SearchProductsQuery command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var adminView = command.AdminView;

        var safePageSize = Math.Min(request.PageSize, 100);
        var safePage     = Math.Max(request.Page, 1);

        var filter = new ProductSearchFilter
        {
            Keyword       = request.Keyword,
            CategoryId    = request.CategoryId,
            Category      = request.Category,
            PriceFrom     = request.PriceFrom,
            PriceTo       = request.PriceTo,
            InStock       = request.InStock,
            AdminView     = adminView,
            SortBy        = request.SortBy,
            SortDirection = request.SortDirection,
            Page          = safePage,
            PageSize      = safePageSize
        };

        var (items, totalCount) = await _productRepository.SearchAsync(filter, cancellationToken);

        var responses = items.Select(p => MapToListItemResponse(p)).ToList();

        return Result.Success(new PagedResponse<ProductListItemResponse>
        {
            Items       = responses,
            TotalCount  = totalCount,
            PageNumber  = safePage,
            PageSize    = safePageSize
        });
    }

    private static ProductListItemResponse MapToListItemResponse(Product p)
    {
        var mainImage = p.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                     ?? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl;

        return new ProductListItemResponse
        {
            Id           = p.Id,
            Name         = p.Name,
            Slug         = p.Slug,
            Price        = p.Price,
            MainImageUrl = mainImage,
            IsOutOfStock = p.QuantityInStock == 0,
            CategoryName = p.Category?.Name ?? string.Empty,
            CategoryId   = p.CategoryId
        };
    }
}
