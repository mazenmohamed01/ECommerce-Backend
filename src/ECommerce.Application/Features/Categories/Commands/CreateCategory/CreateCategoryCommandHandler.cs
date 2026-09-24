using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Categories.Commands.CreateCategory;

internal sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISlugService _slugService;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        ISlugService slugService,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _slugService = slugService;
        _logger = logger;
    }

    public async Task<Result<CategoryResponse>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        string slug;

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var duplicate = await _categoryRepository.SlugExistsAsync(request.Slug, excludeId: null, cancellationToken);

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
                s => _categoryRepository.SlugExistsAsync(s, excludeId: null, cancellationToken),
                cancellationToken);
        }

        var category = Category.Create(
            request.Name,
            slug,
            request.Description,
            imageUrl: null,
            request.SortOrder);

        await _categoryRepository.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category created: {CategoryId} slug={Slug}", category.Id, slug);

        return Result.Success(new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            ProductCount = 0, // newly created category has 0 products
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        });
    }
}
