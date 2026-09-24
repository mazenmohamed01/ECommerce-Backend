using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Categories.Commands.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, Result<CategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlugService _slugService;
    private readonly ILogger<UpdateCategoryCommandHandler> _logger;

    public UpdateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ISlugService slugService,
        ILogger<UpdateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _slugService = slugService;
        _logger = logger;
    }

    public async Task<Result<CategoryResponse>> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var id = command.Id;
        var request = command.Request;

        var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryResponse>(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with ID '{id}' was not found."));

        string slug;

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var duplicate = await _categoryRepository.SlugExistsAsync(request.Slug, excludeId: id, cancellationToken);

            if (duplicate)
                return Result.Failure<CategoryResponse>(Error.Conflict(
                    "CATEGORY_SLUG_DUPLICATE", $"The slug '{request.Slug}' is already in use."));

            slug = request.Slug;
        }
        else
        {
            var baseSlug = _slugService.GenerateSlug(request.Name);
            slug = await _slugService.EnsureUniqueSlugAsync(
                baseSlug,
                s => _categoryRepository.SlugExistsAsync(s, excludeId: id, cancellationToken),
                cancellationToken);
        }

        category.Update(
            request.Name,
            slug,
            request.Description,
            category.ImageUrl, 
            request.SortOrder,
            request.IsActive);

        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category updated: {CategoryId}", id);

        return Result.Success(new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            ProductCount = category.Products?.Count ?? 0,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        });
    }
}
