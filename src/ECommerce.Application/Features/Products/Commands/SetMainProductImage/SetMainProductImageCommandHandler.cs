using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Products.Commands.SetMainProductImage;

internal sealed class SetMainProductImageCommandHandler : ICommandHandler<SetMainProductImageCommand, Result<ProductImageResponse>>
{
    private readonly IProductImageRepository  _imageRepository;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<SetMainProductImageCommandHandler> _logger;

    public SetMainProductImageCommandHandler(
        IProductImageRepository imageRepository,
        IUnitOfWork unitOfWork,
        ILogger<SetMainProductImageCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProductImageResponse>> Handle(SetMainProductImageCommand command, CancellationToken cancellationToken)
    {
        var image = await _imageRepository.GetByIdAsync(command.ImageId, cancellationToken);
        if (image is null || image.ProductId != command.ProductId)
            return Result.Failure<ProductImageResponse>(Error.NotFound(
                "IMAGE_NOT_FOUND", $"Image with ID '{command.ImageId}' was not found for product '{command.ProductId}'."));

        await _imageRepository.UnsetAllMainForProductAsync(command.ProductId, cancellationToken);
        image.SetAsMain();

        await _imageRepository.UpdateAsync(image, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ProductImage {ImageId} set as main for product {ProductId}", command.ImageId, command.ProductId);

        return Result.Success(new ProductImageResponse
        {
            Id           = image.Id,
            ImageUrl     = image.ImageUrl,
            AltText      = image.AltText,
            DisplayOrder = image.DisplayOrder,
            IsMain       = image.IsMain
        });
    }
}
