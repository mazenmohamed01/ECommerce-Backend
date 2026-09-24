using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Cloudinary SDK adapter.
/// Implements <see cref="ICloudinaryService"/> from the Application layer.
/// Wraps all SDK calls — Application code never references CloudinaryDotNet directly.
/// </summary>
public sealed class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary                      _cloudinary;
    private readonly CloudinarySettings              _settings;
    private readonly ILogger<CloudinaryService>      _logger;

    public CloudinaryService(
        IOptions<CloudinarySettings>    options,
        ILogger<CloudinaryService>      logger)
    {
        _settings   = options.Value;
        _logger     = logger;

        var account = new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret);

        _cloudinary = new Cloudinary(account)
        {
            Api = { Secure = true }
        };
    }

    /// <inheritdoc/>
    public async Task<(string PublicId, string SecureUrl)?> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var uploadParams = new ImageUploadParams
            {
                File           = new FileDescription(fileName, fileStream),
                Folder         = folder,
                UseFilename    = false,
                UniqueFilename = true,
                Overwrite      = false,
                // Auto-quality and format optimization
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

            if (result.Error is not null)
            {
                _logger.LogError(
                    "Cloudinary upload error: {Error} (File={FileName})",
                    result.Error.Message, fileName);
                return null;
            }

            _logger.LogInformation(
                "Cloudinary upload succeeded: PublicId={PublicId} Url={Url}",
                result.PublicId, result.SecureUrl);

            return (result.PublicId, result.SecureUrl.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Cloudinary upload (File={FileName})", fileName);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteImageAsync(string publicId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteParams = new DeletionParams(publicId);
            var result       = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Result == "ok")
            {
                _logger.LogInformation("Cloudinary deletion succeeded: PublicId={PublicId}", publicId);
                return true;
            }

            _logger.LogWarning(
                "Cloudinary deletion returned non-ok result: {Result} for PublicId={PublicId}",
                result.Result, publicId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Cloudinary deletion (PublicId={PublicId})", publicId);
            return false;
        }
    }
}
