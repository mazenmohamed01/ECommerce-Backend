using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryBySlug;

internal sealed class GetCategoryBySlugQueryHandler : IQueryHandler<GetCategoryBySlugQuery, Result<CategoryResponse>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryBySlugQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<CategoryResponse>> Handle(GetCategoryBySlugQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (category is null)
            return Result.Failure<CategoryResponse>(Error.NotFound(
                "CATEGORY_NOT_FOUND", $"Category with slug '{request.Slug}' was not found or is inactive."));

        return Result.Success(MapToResponse(category));
    }

    private static CategoryResponse MapToResponse(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        ImageUrl = c.ImageUrl,
        SortOrder = c.SortOrder,
        IsActive = c.IsActive,
        ProductCount = c.Products?.Count ?? 0,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}
