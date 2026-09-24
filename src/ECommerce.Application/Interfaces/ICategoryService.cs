using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Application service contract for category management and browsing.
/// Controllers depend only on this interface — never on the concrete implementation.
/// </summary>
public interface ICategoryService
{
    /// <summary>Creates a new category. Auto-generates slug from Name when not provided.</summary>
    Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing category by ID.</summary>
    Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a category (sets IsActive = false).
    /// Returns Conflict if the category has products.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all active categories ordered by SortOrder (anonymous access).</summary>
    Task<Result<IEnumerable<CategoryResponse>>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all categories including inactive ones (Admin access).</summary>
    Task<Result<IEnumerable<CategoryResponse>>> GetAllForAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a category by its slug. Returns null if not found or inactive.</summary>
    Task<Result<CategoryResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a new hero image for the category to Cloudinary.
    /// Replaces (and deletes from Cloudinary) any existing image.
    /// Upload-first policy: Cloudinary failure returns 502 without touching the DB.
    /// </summary>
    Task<Result<CategoryImageResponse>> UploadImageAsync(
        Guid categoryId,
        UploadCategoryImageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the category hero image from Cloudinary and clears ImageUrl / ImagePublicId.
    /// DB update is applied before attempting Cloudinary deletion (soft-fail policy on Cloudinary).
    /// </summary>
    Task<Result> DeleteImageAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
