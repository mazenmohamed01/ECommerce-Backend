using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ECommerce.Application.Features.Products.Commands.ReorderProductImages;

internal sealed class ReorderProductImagesCommandHandler : ICommandHandler<ReorderProductImagesCommand, Result<IEnumerable<ProductImageResponse>>>
{
    private readonly IProductImageRepository  _imageRepository;
    private readonly IUnitOfWork              _unitOfWork;
    private readonly ILogger<ReorderProductImagesCommandHandler> _logger;

    public ReorderProductImagesCommandHandler(
        IProductImageRepository imageRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReorderProductImagesCommandHandler> logger)
    {
        _imageRepository = imageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<ProductImageResponse>>> Handle(ReorderProductImagesCommand command, CancellationToken cancellationToken)
    {
        var productId = command.ProductId;
        var request = command.Request;

        if (request.ImageOrders is null || request.ImageOrders.Count == 0)
            return Result.Failure<IEnumerable<ProductImageResponse>>(Error.Validation(
                "INVALID_IMAGE_IDS", "ImageOrders list cannot be empty."));

        var productImages = (await _imageRepository.GetByProductIdAsync(productId, cancellationToken))
            .ToDictionary(i => i.Id);

        foreach (var item in request.ImageOrders)
        {
            if (!productImages.ContainsKey(item.ImageId))
                return Result.Failure<IEnumerable<ProductImageResponse>>(Error.Validation(
                    "INVALID_IMAGE_IDS", $"Image '{item.ImageId}' does not belong to product '{productId}'."));
        }

        foreach (var item in request.ImageOrders)
        {
            productImages[item.ImageId].SetDisplayOrder(item.DisplayOrder);
            await _imageRepository.UpdateAsync(productImages[item.ImageId], cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reordered {Count} images for product {ProductId}", request.ImageOrders.Count, productId);

        var responses = productImages.Values
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageResponse
            {
                Id           = i.Id,
                ImageUrl     = i.ImageUrl,
                AltText      = i.AltText,
                DisplayOrder = i.DisplayOrder,
                IsMain       = i.IsMain
            });

        return Result.Success(responses);
    }
}
