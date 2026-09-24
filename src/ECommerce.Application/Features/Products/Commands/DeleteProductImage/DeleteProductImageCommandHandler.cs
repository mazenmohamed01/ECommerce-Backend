using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ECommerce.Application.Features.Products.Commands.DeleteProductImage;

internal sealed class DeleteProductImageCommandHandler : ICommandHandler<DeleteProductImageCommand, Result>
{
    private readonly IProductImageRepository  _imageRepository;
    private readonly ICloudinaryService       _cloudinaryService;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<DeleteProductImageCommandHandler> _logger;

    public DeleteProductImageCommandHandler(
        IProductImageRepository imageRepository,
        ICloudinaryService cloudinaryService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProductImageCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _cloudinaryService = cloudinaryService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteProductImageCommand command, CancellationToken cancellationToken)
    {
        var image = await _imageRepository.GetByIdAsync(command.ImageId, cancellationToken);
        if (image is null || image.ProductId != command.ProductId)
            return Result.Failure(Error.NotFound(
                "IMAGE_NOT_FOUND", $"Image with ID '{command.ImageId}' was not found for product '{command.ProductId}'."));

        var wasMain      = image.IsMain;
        var publicId     = image.CloudinaryPublicId;

        await _imageRepository.DeleteAsync(image, cancellationToken);

        if (wasMain)
        {
            var remaining = (await _imageRepository.GetByProductIdAsync(command.ProductId, cancellationToken))
                .ToList();

            if (remaining.Count > 0)
            {
                remaining[0].SetAsMain();
                await _imageRepository.UpdateAsync(remaining[0], cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var deleted = await _cloudinaryService.DeleteImageAsync(publicId, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning(
                "Cloudinary deletion failed for PublicId={PublicId} (ImageId={ImageId}). " +
                "Orphan asset requires manual cleanup.", publicId, command.ImageId);
        }

        _logger.LogInformation("ProductImage deleted: {ImageId}", command.ImageId);
        return Result.Success();
    }
}
