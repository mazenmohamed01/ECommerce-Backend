using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ECommerce.Application.Features.Products.Commands.UpdateProduct;

internal sealed class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, Result<ProductDetailResponse>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlugService _slugService;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ISlugService slugService,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _slugService = slugService;
        _logger = logger;
    }

    public async Task<Result<ProductDetailResponse>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var id = command.Id;
        var request = command.Request;

        var product = await _productRepository.GetByIdAsync(id, includeImages: true, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDetailResponse>(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with ID '{id}' was not found."));

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result.Failure<ProductDetailResponse>(Error.Validation(
                "CATEGORY_NOT_FOUND_OR_INACTIVE", "The specified category does not exist or is not active."));

        var slugResult = await ResolveSlugAsync(
            request.Slug, request.Name, excludeId: id, cancellationToken);
        if (slugResult.IsFailure)
            return Result.Failure<ProductDetailResponse>(slugResult.Error);
            
        var slug = slugResult.Value;

        product.Update(
            request.CategoryId,
            request.Name,
            slug,
            request.Sku,
            request.Price,
            request.QuantityInStock,
            request.Description,
            request.IsActive);

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product updated: {ProductId}", id);

        return Result.Success(MapToDetailResponse(product, category.Name));
    }

    private async Task<Result<string>> ResolveSlugAsync(
        string? providedSlug,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(providedSlug))
        {
            var duplicate = await _productRepository.SlugExistsAsync(
                providedSlug, excludeId, cancellationToken);

            if (duplicate)
                return Result.Failure<string>(Error.Conflict(
                    "PRODUCT_SLUG_DUPLICATE", $"The slug '{providedSlug}' is already in use."));

            return Result.Success(providedSlug);
        }

        var baseSlug = _slugService.GenerateSlug(name);
        var generatedSlug = await _slugService.EnsureUniqueSlugAsync(
            baseSlug,
            s => _productRepository.SlugExistsAsync(s, excludeId, cancellationToken),
            cancellationToken);
            
        return Result.Success(generatedSlug);
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
