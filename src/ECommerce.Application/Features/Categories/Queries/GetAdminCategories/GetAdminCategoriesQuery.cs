using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System.Collections.Generic;

namespace ECommerce.Application.Features.Categories.Queries.GetAdminCategories;

public sealed record GetAdminCategoriesQuery() : IQuery<Result<IEnumerable<CategoryResponse>>>;
