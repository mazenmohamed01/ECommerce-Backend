using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;

namespace ECommerce.Application.Features.Categories.Queries.GetAdminCategories;

internal sealed class GetAdminCategoriesQueryHandler : IQueryHandler<GetAdminCategoriesQuery, Result<IEnumerable<CategoryResponse>>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetAdminCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<IEnumerable<CategoryResponse>>> Handle(GetAdminCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllForAdminAsync(cancellationToken);
        return Result.Success(categories.Select(MapToResponse));
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
