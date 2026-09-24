using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Common;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Services;

/// <summary>
/// Orchestrates product image upload, deletion, main-image promotion, and display-order reordering.
/// Upload is Cloudinary-first: if Cloudinary fails, no DB row is created (502 returned).
/// Delete is DB-first: Cloudinary failure is logged but does not block the admin operation.
/// </summary>
public sealed class ProductImageService : IProductImageService
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp"
    };

    // Magic byte signatures for allowed image types
    private static readonly byte[] JpegMagic  = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic   = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] WebPMagic  = [0x52, 0x49, 0x46, 0x46]; // RIFF header (WebP)

    private readonly IProductRepository       _productRepository;
    private readonly IProductImageRepository  _imageRepository;
    private readonly ICloudinaryService       _cloudinaryService;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<ProductImageService> _logger;
    private readonly IValidator<UploadProductImageRequest> _uploadValidator;

    private const string CloudinaryFolder = "ecommerce/products";

    public ProductImageService(
        IProductRepository                     productRepository,
        IProductImageRepository                imageRepository,
        ICloudinaryService                     cloudinaryService,
        IUnitOfWork                            unitOfWork,
        ILogger<ProductImageService>           logger,
        IValidator<UploadProductImageRequest>  uploadValidator)
    {
        _productRepository  = productRepository;
        _imageRepository    = imageRepository;
        _cloudinaryService  = cloudinaryService;
        _unitOfWork         = unitOfWork;
        _logger             = logger;
        _uploadValidator    = uploadValidator;
    }

    /// <inheritdoc/>
    public async Task<Result<ProductImageResponse>> UploadAsync(
        Guid productId,
        UploadProductImageRequest request,
        CancellationToken cancellationToken = default)
    {
        // ── Structural validation ──────────────────────────────────────────────
        var validation = await _uploadValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure<ProductImageResponse>(Error.Validation(
                "VALIDATION_ERROR", 
                string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        // ── Product must exist (any state — admin can upload to inactive products) ─
        var exists = await _productRepository.ExistsAsync(productId, cancellationToken);
        if (!exists)
            return Result.Failure<ProductImageResponse>(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with ID '{productId}' was not found."));

        // ── Magic-byte validation (prevents disguised uploads) ────────────────
        await using var stream = request.File.OpenReadStream();
        if (!await IsValidImageSignatureAsync(stream, cancellationToken))
            return Result.Failure<ProductImageResponse>(Error.Validation(
                "INVALID_FILE_TYPE", "The uploaded file does not match its declared content type."));

        stream.Seek(0, SeekOrigin.Begin);

        // ── Upload to Cloudinary first (Cloudinary-first policy) ─────────────
        var uploadResult = await _cloudinaryService.UploadImageAsync(
            stream,
            request.File.FileName,
            CloudinaryFolder,
            cancellationToken);

        if (uploadResult is null)
            return Result.Failure<ProductImageResponse>(Error.BadGateway(
                "IMAGE_UPLOAD_FAILED", "The image could not be uploaded to the storage provider. Please try again."));

        var (publicId, secureUrl) = uploadResult.Value;

        // ── Determine DisplayOrder (next after current max) ───────────────────
        var maxOrder    = await _imageRepository.GetMaxDisplayOrderAsync(productId, cancellationToken);
        var displayOrder = maxOrder + 1;

        // ── Enforce single-Main invariant ─────────────────────────────────────
        if (request.IsMain)
            await _imageRepository.UnsetAllMainForProductAsync(productId, cancellationToken);

        // ── Persist DB row ────────────────────────────────────────────────────
        var image = ProductImage.Create(
            productId,
            secureUrl,
            publicId,
            displayOrder,
            request.IsMain,
            request.AltText);

        await _imageRepository.AddAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ProductImage created: {ImageId} for product {ProductId} (IsMain={IsMain})",
            image.Id, productId, request.IsMain);

        return Result.Success(MapToResponse(image));
    }

    /// <inheritdoc/>
    public async Task<Result> DeleteAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var image = await _imageRepository.GetByIdAsync(imageId, cancellationToken);
        if (image is null || image.ProductId != productId)
            return Result.Failure(Error.NotFound(
                "IMAGE_NOT_FOUND", $"Image with ID '{imageId}' was not found for product '{productId}'."));

        var wasMain      = image.IsMain;
        var publicId     = image.CloudinaryPublicId;

        // ── Remove DB row first ───────────────────────────────────────────────
        await _imageRepository.DeleteAsync(image, cancellationToken);

        // ── Promote next image to Main if needed ──────────────────────────────
        if (wasMain)
        {
            var remaining = (await _imageRepository.GetByProductIdAsync(productId, cancellationToken))
                .ToList();

            if (remaining.Count > 0)
            {
                // The repository returns images ordered by DisplayOrder ASC
                remaining[0].SetAsMain();
                await _imageRepository.UpdateAsync(remaining[0], cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ── Attempt Cloudinary deletion (soft-fail policy) ────────────────────
        var deleted = await _cloudinaryService.DeleteImageAsync(publicId, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning(
                "Cloudinary deletion failed for PublicId={PublicId} (ImageId={ImageId}). " +
                "Orphan asset requires manual cleanup.", publicId, imageId);
        }

        _logger.LogInformation("ProductImage deleted: {ImageId}", imageId);
        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<ProductImageResponse>> SetMainAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        var image = await _imageRepository.GetByIdAsync(imageId, cancellationToken);
        if (image is null || image.ProductId != productId)
            return Result.Failure<ProductImageResponse>(Error.NotFound(
                "IMAGE_NOT_FOUND", $"Image with ID '{imageId}' was not found for product '{productId}'."));

        // Unset all, then set the target
        await _imageRepository.UnsetAllMainForProductAsync(productId, cancellationToken);
        image.SetAsMain();

        await _imageRepository.UpdateAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ProductImage {ImageId} set as main for product {ProductId}", imageId, productId);

        return Result.Success(MapToResponse(image));
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<ProductImageResponse>>> ReorderAsync(
        Guid productId,
        ReorderProductImagesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ImageOrders is null || request.ImageOrders.Count == 0)
            return Result.Failure<IEnumerable<ProductImageResponse>>(Error.Validation(
                "INVALID_IMAGE_IDS", "ImageOrders list cannot be empty."));

        // Load all images for this product
        var productImages = (await _imageRepository.GetByProductIdAsync(productId, cancellationToken))
            .ToDictionary(i => i.Id);

        // Validate that every submitted ImageId belongs to this product
        foreach (var item in request.ImageOrders)
        {
            if (!productImages.ContainsKey(item.ImageId))
                return Result.Failure<IEnumerable<ProductImageResponse>>(Error.Validation(
                    "INVALID_IMAGE_IDS", $"Image '{item.ImageId}' does not belong to product '{productId}'."));
        }

        // Apply new display orders
        foreach (var item in request.ImageOrders)
        {
            productImages[item.ImageId].SetDisplayOrder(item.DisplayOrder);
            await _imageRepository.UpdateAsync(productImages[item.ImageId], cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reordered {Count} images for product {ProductId}", request.ImageOrders.Count, productId);

        return Result.Success(productImages.Values
            .OrderBy(i => i.DisplayOrder)
            .Select(MapToResponse));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static async Task<bool> IsValidImageSignatureAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var read   = await stream.ReadAsync(header.AsMemory(0, 8), cancellationToken);

        if (read < 3) return false;

        // JPEG: FF D8 FF
        if (header[0] == JpegMagic[0] && header[1] == JpegMagic[1] && header[2] == JpegMagic[2])
            return true;

        // PNG: 89 50 4E 47
        if (read >= 4 &&
            header[0] == PngMagic[0] && header[1] == PngMagic[1] &&
            header[2] == PngMagic[2] && header[3] == PngMagic[3])
            return true;

        // WebP: RIFF????WEBP — bytes 0-3 = RIFF, bytes 8-11 = WEBP
        // For magic-byte check we check the RIFF header at offset 0
        if (read >= 4 &&
            header[0] == WebPMagic[0] && header[1] == WebPMagic[1] &&
            header[2] == WebPMagic[2] && header[3] == WebPMagic[3])
            return true;

        return false;
    }

    private static ProductImageResponse MapToResponse(ProductImage i) => new()
    {
        Id           = i.Id,
        ImageUrl     = i.ImageUrl,
        AltText      = i.AltText,
        DisplayOrder = i.DisplayOrder,
        IsMain       = i.IsMain
    };
}
