using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

/// <summary>
/// Category-specific repository contract.
/// Extends the generic IRepository with domain-driven query methods.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>Returns a category by its URL slug (case-insensitive).</summary>
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a slug already exists in the table,
    /// optionally excluding the category being updated.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns true if at least one product references this category (even soft-deleted products).</summary>
    Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>Returns all categories where IsActive = true, ordered by SortOrder then Name.</summary>
    Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all categories regardless of IsActive status (Admin view).</summary>
    Task<IEnumerable<Category>> GetAllForAdminAsync(CancellationToken cancellationToken = default);
}
