using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ECommerce.Application.Features.Products.Commands.UploadProductImage;

internal sealed class UploadProductImageCommandHandler : ICommandHandler<UploadProductImageCommand, Result<ProductImageResponse>>
{
    private static readonly byte[] JpegMagic  = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic   = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] WebPMagic  = [0x52, 0x49, 0x46, 0x46]; // RIFF

    private const string CloudinaryFolder = "ecommerce/products";

    private readonly IProductRepository       _productRepository;
    private readonly IProductImageRepository  _imageRepository;
    private readonly ICloudinaryService       _cloudinaryService;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<UploadProductImageCommandHandler> _logger;

    public UploadProductImageCommandHandler(
        IProductRepository productRepository,
        IProductImageRepository imageRepository,
        ICloudinaryService cloudinaryService,
        IUnitOfWork unitOfWork,
        ILogger<UploadProductImageCommandHandler> logger)
    {
        _productRepository = productRepository;
        _imageRepository = imageRepository;
        _cloudinaryService = cloudinaryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductImageResponse>> Handle(UploadProductImageCommand command, CancellationToken cancellationToken)
    {
        var productId = command.ProductId;
        var request = command.Request;

        var exists = await _productRepository.ExistsAsync(productId, cancellationToken);
        if (!exists)
            return Result.Failure<ProductImageResponse>(Error.NotFound(
                "PRODUCT_NOT_FOUND", $"Product with ID '{productId}' was not found."));

        await using var stream = request.File.OpenReadStream();
        if (!await IsValidImageSignatureAsync(stream, cancellationToken))
            return Result.Failure<ProductImageResponse>(Error.Validation(
                "INVALID_FILE_TYPE", "The uploaded file does not match its declared content type."));

        stream.Seek(0, SeekOrigin.Begin);

        var uploadResult = await _cloudinaryService.UploadImageAsync(
            stream,
            request.File.FileName,
            CloudinaryFolder,
            cancellationToken);

        if (uploadResult is null)
            return Result.Failure<ProductImageResponse>(Error.BadGateway(
                "IMAGE_UPLOAD_FAILED", "The image could not be uploaded to the storage provider. Please try again."));

        var (publicId, secureUrl) = uploadResult.Value;

        var maxOrder = await _imageRepository.GetMaxDisplayOrderAsync(productId, cancellationToken);
        var displayOrder = maxOrder + 1;

        if (request.IsMain)
            await _imageRepository.UnsetAllMainForProductAsync(productId, cancellationToken);

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

    private static async Task<bool> IsValidImageSignatureAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        var header = new byte[8];
        var read   = await stream.ReadAsync(header.AsMemory(0, 8), cancellationToken);

        if (read < 3) return false;

        if (header[0] == JpegMagic[0] && header[1] == JpegMagic[1] && header[2] == JpegMagic[2])
            return true;

        if (read >= 4 &&
            header[0] == PngMagic[0] && header[1] == PngMagic[1] &&
            header[2] == PngMagic[2] && header[3] == PngMagic[3])
            return true;

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
