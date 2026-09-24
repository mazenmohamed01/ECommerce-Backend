using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IProductImageRepository"/>.
/// </summary>
public sealed class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
{
    public ProductImageRepository(ApplicationDbContext context) : base(context) { }

    /// <inheritdoc/>
    public async Task<IEnumerable<ProductImage>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
        => await DbSet
            .Where(i => i.ProductId == productId)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public new async Task<ProductImage?> GetByIdAsync(
        Guid imageId,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(i => i.Id == imageId, cancellationToken);

    /// <inheritdoc/>
    public async Task<ProductImage?> GetMainImageAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(
            i => i.ProductId == productId && i.IsMain,
            cancellationToken);

    /// <inheritdoc/>
    public async Task UnsetAllMainForProductAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        // Batch update via ExecuteUpdateAsync — avoids loading all images into memory
        await DbSet
            .Where(i => i.ProductId == productId && i.IsMain)
            .ExecuteUpdateAsync(
                s => s.SetProperty(i => i.IsMain, false),
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetMaxDisplayOrderAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var hasImages = await DbSet.AnyAsync(i => i.ProductId == productId, cancellationToken);
        if (!hasImages) return -1;

        return await DbSet
            .Where(i => i.ProductId == productId)
            .MaxAsync(i => i.DisplayOrder, cancellationToken);
    }
}
