using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System.Collections.Generic;

namespace ECommerce.Application.Features.Categories.Queries.GetActiveCategories;

public sealed record GetActiveCategoriesQuery() : IQuery<Result<IEnumerable<CategoryResponse>>>;
