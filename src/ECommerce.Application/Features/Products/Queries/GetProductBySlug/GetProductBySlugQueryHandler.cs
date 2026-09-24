using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using System.Linq;

namespace ECommerce.Application.Features.Products.Queries.GetProductBySlug;

internal sealed class GetProductBySlugQueryHandler : IQueryHandler<GetProductBySlugQuery, Result<ProductDetailResponse>>
{
    private readonly IProductRepository _productRepository;

    public GetProductBySlugQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductDetailResponse>> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDetailResponse>(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with slug '{request.Slug}' was not found or is inactive."));

        return Result.Success(MapToDetailResponse(product, product.Category.Name));
    }

    private static ProductDetailResponse MapToDetailResponse(Product p, string categoryName)
    {
        var mainImage = p.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                     ?? p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl;

        return new ProductDetailResponse
        {
            Id              = p.Id,
            CategoryId      = p.CategoryId,
            CategoryName    = categoryName,
            Name            = p.Name,
            Slug            = p.Slug,
            Sku             = p.Sku,
            Description     = p.Description,
            Price           = p.Price,
            QuantityInStock = p.QuantityInStock,
            IsActive        = p.IsActive,
            IsOutOfStock    = p.QuantityInStock == 0,
            MainImageUrl    = mainImage,
            Images          = p.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(MapToImageResponse)
                .ToList()
                .AsReadOnly(),
            CreatedAt       = p.CreatedAt,
            UpdatedAt       = p.UpdatedAt,
            DeletedAt       = p.DeletedAt
        };
    }

    private static ProductImageResponse MapToImageResponse(ProductImage i) => new()
    {
        Id           = i.Id,
        ImageUrl     = i.ImageUrl,
        AltText      = i.AltText,
        DisplayOrder = i.DisplayOrder,
        IsMain       = i.IsMain
    };
}
