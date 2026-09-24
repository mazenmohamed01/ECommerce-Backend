using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="ICategoryRepository"/>.
/// Extends the generic Repository with category-specific query methods.
/// </summary>
public sealed class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<Category?> GetBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(
                c => EF.Functions.ILike(c.Slug, slug),
                cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> SlugExistsAsync(
        string slug,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(
            c => EF.Functions.ILike(c.Slug, slug) && (excludeId == null || c.Id != excludeId),
            cancellationToken);

    /// <inheritdoc/>
    public async Task<bool> HasProductsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
        => await Context.Products
            .AnyAsync(p => p.CategoryId == categoryId, cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<Category>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(c => c.Products)
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IEnumerable<Category>> GetAllForAdminAsync(
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(c => c.Products)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Override base GetByIdAsync to include the Products navigation
    /// so CategoryService.MapToResponse can compute ProductCount.
    /// </summary>
    public override async Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
}
