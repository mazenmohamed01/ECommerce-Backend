using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

/// <summary>
/// Product-specific repository contract.
/// Extends the generic IRepository with domain-driven query methods for catalog browsing.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Returns a product by ID, optionally including its images collection.
    /// </summary>
    Task<Product?> GetByIdAsync(
        Guid id,
        bool includeImages,
        CancellationToken cancellationToken = default);

    /// <summary>Returns an active product by its URL slug (case-insensitive). Returns null if soft-deleted.</summary>
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a slug already exists in the table,
    /// optionally excluding the product being updated.
    /// </summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dynamic search with filtering, sorting, and pagination.
    /// Returns a page of products and the total count before pagination.
    /// </summary>
    Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true if a product with this ID exists (any state).</summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    // ── Phase 3: Stock Concurrency ──────────────────────────────────────────

    /// <summary>
    /// Issues a raw SQL SELECT ... FOR UPDATE to exclusively lock the product row.
    /// Prevents race conditions during concurrent checkouts. Must run in a transaction.
    /// </summary>
    Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely updates stock or reservation quantities.
    /// </summary>
    Task UpdateStockAsync(Guid id, int quantityDelta, int reservedDelta, CancellationToken cancellationToken = default);
}

/// <summary>
/// Internal search filter model passed from service to repository.
/// Not a DTO — never crosses the API boundary directly.
/// </summary>
public sealed class ProductSearchFilter
{
    public string?  Keyword       { get; init; }
    public Guid?    CategoryId    { get; init; }
    public string?  Category      { get; init; }
    public decimal? PriceFrom     { get; init; }
    public decimal? PriceTo       { get; init; }
    public bool?    InStock       { get; init; }
    public bool     AdminView     { get; init; }  // when true, includes inactive products
    public string   SortBy        { get; init; } = "Newest";
    public string   SortDirection { get; init; } = "desc";
    public int      Page          { get; init; } = 1;
    public int      PageSize      { get; init; } = 20;
}
