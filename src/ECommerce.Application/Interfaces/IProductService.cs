using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Application service contract for product management and public catalog browsing.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Creates a new product. Validates category is active. Auto-generates slug from Name when absent.
    /// Returns Failure if category is inactive/missing or slug is duplicate.
    /// </summary>
    Task<Result<ProductDetailResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product by ID (Admin).
    /// Returns NotFound if product not found.
    /// </summary>
    Task<Result<ProductDetailResponse>> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a product (IsActive = false, DeletedAt = now).
    /// Returns NotFound if product not found.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns full product detail by ID (Admin — includes inactive products).</summary>
    Task<Result<ProductDetailResponse>> GetByIdForAdminAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns full product detail by slug (Public — returns NotFound if inactive or not found).
    /// </summary>
    Task<Result<ProductDetailResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products with dynamic filtering, sorting, and pagination.
    /// Only returns active products when called from public context (adminView = false).
    /// </summary>
    Task<Result<PagedResponse<ProductListItemResponse>>> SearchAsync(
        ProductSearchRequest request,
        bool adminView = false,
        CancellationToken cancellationToken = default);
}
