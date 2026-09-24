using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Orchestrates product CRUD, slug management, category validation, and public catalog search.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository              _productRepository;
    private readonly ICategoryRepository             _categoryRepository;
    private readonly IUnitOfWork                     _unitOfWork;
    private readonly ISlugService                    _slugService;
    private readonly ILogger<ProductService>         _logger;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;

    public ProductService(
        IProductRepository               productRepository,
        ICategoryRepository              categoryRepository,
        IUnitOfWork                      unitOfWork,
        ISlugService                     slugService,
        ILogger<ProductService>          logger,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator)
    {
        _productRepository  = productRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork         = unitOfWork;
        _slugService        = slugService;
        _logger             = logger;
        _createValidator    = createValidator;
        _updateValidator    = updateValidator;
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDetailResponse>> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Structural validation ──────────────────────────────────────────────
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<ProductDetailResponse>(Error.Validation(
                "VALIDATION_ERROR", 
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Business rule: category must exist and be active ──────────────────
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result.Failure<ProductDetailResponse>(Error.Validation(
                "CATEGORY_NOT_FOUND_OR_INACTIVE", "The specified category does not exist or is not active."));

        // ── Resolve slug ──────────────────────────────────────────────────────
        var slugResult = await ResolveSlugAsync(
            request.Slug, request.Name, excludeId: null, cancellationToken);
        if (slugResult.IsFailure)
            return Result.Failure<ProductDetailResponse>(slugResult.Error);
            
        var slug = slugResult.Value;

        // ── Persist ───────────────────────────────────────────────────────────
        var product = Product.Create(
            request.CategoryId,
            request.Name,
            slug,
            request.Sku,
            request.Price,
            request.QuantityInStock,
            request.Description);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Product created: {ProductId} slug={Slug} sku={Sku}", product.Id, slug, request.Sku);

        // Reload with images and category navigation
        var created = await _productRepository.GetByIdAsync(product.Id, includeImages: true, cancellationToken)
                      ?? product;

        return Result.Success(MapToDetailResponse(created, category.Name));
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDetailResponse>> UpdateAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Structural validation ──────────────────────────────────────────────
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<ProductDetailResponse>(Error.Validation(
                "VALIDATION_ERROR", 
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Load product ──────────────────────────────────────────────────────
        var product = await _productRepository.GetByIdAsync(id, includeImages: true, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDetailResponse>(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with ID '{id}' was not found."));

        // ── Business rule: target category must exist and be active ───────────
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
            return Result.Failure<ProductDetailResponse>(Error.Validation(
                "CATEGORY_NOT_FOUND_OR_INACTIVE", "The specified category does not exist or is not active."));

        // ── Resolve slug ──────────────────────────────────────────────────────
        var slugResult = await ResolveSlugAsync(
            request.Slug, request.Name, excludeId: id, cancellationToken);
        if (slugResult.IsFailure)
            return Result.Failure<ProductDetailResponse>(slugResult.Error);
            
        var slug = slugResult.Value;

        // ── Apply changes ─────────────────────────────────────────────────────
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

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, includeImages: false, cancellationToken);
        if (product is null)
            return Result.Failure(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with ID '{id}' was not found."));

        product.SoftDelete();

        await _productRepository.UpdateAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product soft-deleted: {ProductId} at {DeletedAt}", id, product.DeletedAt);
        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDetailResponse>> GetByIdForAdminAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(id, includeImages: true, cancellationToken);
        if (product is null) 
            return Result.Failure<ProductDetailResponse>(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with ID '{id}' was not found."));

        return Result.Success(MapToDetailResponse(product, product.Category.Name));
    }

    /// <inheritdoc/>
    public async Task<Result<ProductDetailResponse>> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetBySlugAsync(slug, cancellationToken);
        if (product is null)
            return Result.Failure<ProductDetailResponse>(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with slug '{slug}' was not found or is inactive."));

        return Result.Success(MapToDetailResponse(product, product.Category.Name));
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResponse<ProductListItemResponse>>> SearchAsync(
        ProductSearchRequest request,
        bool adminView = false,
        CancellationToken cancellationToken = default)
    {
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

    // ── Private helpers ────────────────────────────────────────────────────────

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

    private static ProductImageResponse MapToImageResponse(ProductImage i) => new()
    {
        Id           = i.Id,
        ImageUrl     = i.ImageUrl,
        AltText      = i.AltText,
        DisplayOrder = i.DisplayOrder,
        IsMain       = i.IsMain
    };
}
