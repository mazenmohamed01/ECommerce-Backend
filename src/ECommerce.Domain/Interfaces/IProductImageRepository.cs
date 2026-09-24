using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

/// <summary>
/// ProductImage-specific repository contract.
/// Images are children of the Product aggregate and are managed through this repository.
/// </summary>
public interface IProductImageRepository : IRepository<ProductImage>
{
    /// <summary>Returns all images for a product ordered by DisplayOrder ascending.</summary>
    Task<IEnumerable<ProductImage>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single image by its own Id.</summary>
    new Task<ProductImage?> GetByIdAsync(Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>Returns the image marked as IsMain for the given product, or null if none.</summary>
    Task<ProductImage?> GetMainImageAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets IsMain = false on every image belonging to the product.
    /// Called before promoting a new image to Main to ensure at-most-one invariant.
    /// </summary>
    Task UnsetAllMainForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the highest DisplayOrder value for images of the given product.
    /// Returns -1 if the product has no images yet (so first image gets DisplayOrder 0).
    /// </summary>
    Task<int> GetMaxDisplayOrderAsync(Guid productId, CancellationToken cancellationToken = default);
}
