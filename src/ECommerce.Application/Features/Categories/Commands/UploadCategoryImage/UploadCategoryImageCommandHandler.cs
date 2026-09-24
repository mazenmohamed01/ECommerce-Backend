using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ECommerce.Application.Features.Categories.Commands.UploadCategoryImage;

internal sealed class UploadCategoryImageCommandHandler : ICommandHandler<UploadCategoryImageCommand, Result<CategoryImageResponse>>
{
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic  = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] WebPMagic = [0x52, 0x49, 0x46, 0x46]; // RIFF

    private const string CloudinaryFolder = "ecommerce/categories";

    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<UploadCategoryImageCommandHandler> _logger;

    public UploadCategoryImageCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        ILogger<UploadCategoryImageCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    public async Task<Result<CategoryImageResponse>> Handle(UploadCategoryImageCommand command, CancellationToken cancellationToken)
    {
        var categoryId = command.CategoryId;
        var request = command.Request;

        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryImageResponse>(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with ID '{categoryId}' was not found."));

        await using var stream = request.File.OpenReadStream();
        var signatureValid = await IsValidImageSignatureAsync(stream, cancellationToken);
        if (!signatureValid)
            return Result.Failure<CategoryImageResponse>(Error.Validation(
                "INVALID_IMAGE_FORMAT",
                "The uploaded file does not appear to be a valid JPEG, PNG, or WebP image."));

        stream.Seek(0, SeekOrigin.Begin);

        if (!string.IsNullOrWhiteSpace(category.ImagePublicId))
        {
            var deleted = await _cloudinaryService.DeleteImageAsync(
                category.ImagePublicId, cancellationToken);

            if (!deleted)
                _logger.LogWarning(
                    "Could not delete old Cloudinary image {PublicId} for category {CategoryId}",
                    category.ImagePublicId, categoryId);
        }

        var uploadResult = await _cloudinaryService.UploadImageAsync(
            stream,
            request.File.FileName,
            CloudinaryFolder,
            cancellationToken);

        if (uploadResult is null)
            return Result.Failure<CategoryImageResponse>(Error.BadGateway(
                "CLOUDINARY_UPLOAD_FAILED",
                "Image upload to Cloudinary failed. Please try again later."));

        category.UpdateImage(uploadResult.Value.SecureUrl, uploadResult.Value.PublicId);
        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Category image uploaded: {CategoryId} publicId={PublicId}",
            categoryId, uploadResult.Value.PublicId);

        return Result.Success(new CategoryImageResponse
        {
            ImageUrl = uploadResult.Value.SecureUrl,
            AltText  = request.AltText
        });
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
}
