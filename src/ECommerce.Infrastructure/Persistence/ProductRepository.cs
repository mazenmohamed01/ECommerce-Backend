using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IProductRepository"/>.
/// The SearchAsync method composes an IQueryable dynamically — no raw SQL.
/// </summary>
public sealed class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Product?> GetByIdAsync(
        Guid id,
        bool includeImages,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(p => p.Category)
            .AsQueryable();

        if (includeImages)
            query = query.Include(p => p.Images);

        return await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Product?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(
                p => p.IsActive && EF.Functions.ILike(p.Slug, slug),
                cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(
            p => EF.Functions.ILike(p.Slug, slug) && (excludeId == null || p.Id != excludeId),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<(IEnumerable<Product> Items, int TotalCount)> SearchAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        // ── Base query ─────────────────────────────────────────────────────────
        var query = DbSet
            .Include(p => p.Category)
            .Include(p => p.Images)
            .AsQueryable();

        // ── Implicit active filter for public access ───────────────────────────
        if (!filter.AdminView)
            query = query.Where(p => p.IsActive);

        // ── Dynamic filters ────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = $"%{filter.Keyword}%";
            query  = query.Where(p =>
                EF.Functions.ILike(p.Name, kw) ||
                (p.Description != null && EF.Functions.ILike(p.Description, kw)));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(p => EF.Functions.ILike(p.Category!.Slug, filter.Category) || EF.Functions.ILike(p.Category!.Name, filter.Category));

        if (filter.PriceFrom.HasValue)
            query = query.Where(p => p.Price >= filter.PriceFrom.Value);

        if (filter.PriceTo.HasValue)
            query = query.Where(p => p.Price <= filter.PriceTo.Value);

        if (filter.InStock.HasValue)
        {
            query = filter.InStock.Value
                ? query.Where(p => p.QuantityInStock > 0)
                : query.Where(p => p.QuantityInStock == 0);
        }

        // ── Count before pagination (lightweight count query) ──────────────────
        var totalCount = await query.CountAsync(cancellationToken);

        // ── Sorting ────────────────────────────────────────────────────────────
        query = filter.SortBy.ToLowerInvariant() switch
        {
            "name"   => filter.SortDirection.ToLowerInvariant() == "asc"
                ? query.OrderBy(p => p.Name)
                : query.OrderByDescending(p => p.Name),

            "price"  => filter.SortDirection.ToLowerInvariant() == "asc"
                ? query.OrderBy(p => p.Price)
                : query.OrderByDescending(p => p.Price),

            "bestselling" or "popular" => query
                .GroupJoin(
                    Context.OrderItems,
                    p => p.Id,
                    oi => oi.ProductId,
                    (p, orderItems) => new { Product = p, TotalSold = orderItems.Sum(x => (int?)x.Quantity) ?? 0 }
                )
                .OrderByDescending(x => x.TotalSold)
                .ThenByDescending(x => x.Product.CreatedAt)
                .Select(x => x.Product),

            "oldest" => query.OrderBy(p => p.CreatedAt),

            _        => query.OrderByDescending(p => p.CreatedAt) // "newest" = default
        };

        // ── Pagination ─────────────────────────────────────────────────────────
        var safePageSize = Math.Min(Math.Max(filter.PageSize, 1), 100);
        var safePage     = Math.Max(filter.Page, 1);

        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(p => p.Id == id, cancellationToken);

    // ── Phase 3: Stock Concurrency ──────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Product?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Deadlock Protection: Configure fail-fast behavior with a 2-second lock timeout.
        await Context.Database.ExecuteSqlRawAsync("SET LOCAL lock_timeout = '2s';", cancellationToken);

        // Issue the SELECT FOR UPDATE via raw SQL to acquire the PostgreSQL row-level lock.
        // We discard the reader result here — this is purely to hold the lock for the duration of the transaction.
        // Using FromSqlInterpolated with SELECT * wraps the query in a subquery alias (e.xmin issue in Npgsql),
        // so instead we use ExecuteSqlRawAsync to acquire the lock and then re-load via the standard EF tracked query.
        await Context.Database.ExecuteSqlRawAsync(
            "SELECT 1 FROM \"Products\" WHERE \"Id\" = {0} FOR UPDATE",
            new object[] { id },
            cancellationToken);

        // Re-load the entity via EF Core's standard pipeline for proper change-tracking and mapping.
        // The row lock established above is still held within this transaction.
        return await DbSet.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateStockAsync(Guid id, int quantityDelta, int reservedDelta, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.QuantityInStock, p => p.QuantityInStock + quantityDelta)
                .SetProperty(p => p.ReservedStock, p => p.ReservedStock + reservedDelta),
                cancellationToken);
    }
}
