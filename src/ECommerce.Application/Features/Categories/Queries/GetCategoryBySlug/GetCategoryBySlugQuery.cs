using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Categories.Queries.GetCategoryBySlug;

public sealed record GetCategoryBySlugQuery(string Slug) : IQuery<Result<CategoryResponse>>;
