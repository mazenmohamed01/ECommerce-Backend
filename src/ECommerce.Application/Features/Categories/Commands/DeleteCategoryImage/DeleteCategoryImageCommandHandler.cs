using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Categories.Commands.DeleteCategoryImage;

internal sealed class DeleteCategoryImageCommandHandler : ICommandHandler<DeleteCategoryImageCommand, Result>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<DeleteCategoryImageCommandHandler> _logger;

    public DeleteCategoryImageCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        ILogger<DeleteCategoryImageCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteCategoryImageCommand command, CancellationToken cancellationToken)
    {
        var categoryId = command.CategoryId;

        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            return Result.Failure(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with ID '{categoryId}' was not found."));

        if (string.IsNullOrWhiteSpace(category.ImageUrl))
            return Result.Failure(Error.Validation(
                "CATEGORY_NO_IMAGE", "This category does not have an image to delete."));

        var publicId = category.ImagePublicId;

        category.RemoveImage();
        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(publicId))
        {
            var deleted = await _cloudinaryService.DeleteImageAsync(publicId, cancellationToken);
            if (!deleted)
                _logger.LogWarning(
                    "DB cleared but Cloudinary deletion failed for publicId={PublicId} (category {CategoryId})",
                    publicId, categoryId);
        }

        _logger.LogInformation("Category image deleted: {CategoryId}", categoryId);
        return Result.Success();
    }
}
