namespace ECommerce.Application.Interfaces;

/// <summary>
/// Abstraction over the Cloudinary image hosting service.
/// Implemented in Infrastructure — Application services depend only on this interface.
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Uploads an image stream to Cloudinary under the specified folder.
    /// Returns the (PublicId, SecureUrl) pair on success.
    /// Returns null on failure (caller should return 502 Bad Gateway).
    /// </summary>
    Task<(string PublicId, string SecureUrl)?> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from Cloudinary by its PublicId.
    /// Returns true if deleted successfully, false otherwise.
    /// Callers should log failures but not block on them (soft-delete policy).
    /// </summary>
    Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default);
}
