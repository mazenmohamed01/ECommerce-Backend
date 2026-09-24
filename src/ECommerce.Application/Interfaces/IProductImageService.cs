using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Application service contract for managing product images.
/// Coordinates Cloudinary upload/deletion and enforces the IsMain and DisplayOrder invariants.
/// </summary>
public interface IProductImageService
{
    /// <summary>
    /// Uploads an image to Cloudinary and persists the ProductImage row.
    /// If IsMain = true, unsets any existing main image first.
    /// </summary>
    Task<Result<ProductImageResponse>> UploadAsync(
        Guid productId,
        UploadProductImageRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image row and triggers a Cloudinary deletion.
    /// If the deleted image was Main and other images exist, promotes the next lowest DisplayOrder image.
    /// </summary>
    Task<Result> DeleteAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the specified image as IsMain for the product.
    /// Unsets all other images' IsMain flag in the same transaction.
    /// </summary>
    Task<Result<ProductImageResponse>> SetMainAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-updates DisplayOrder for a product's images.
    /// Validates all imageIds belong to the product before applying.
    /// </summary>
    Task<Result<IEnumerable<ProductImageResponse>>> ReorderAsync(
        Guid productId,
        ReorderProductImagesRequest request,
        CancellationToken cancellationToken = default);
}
