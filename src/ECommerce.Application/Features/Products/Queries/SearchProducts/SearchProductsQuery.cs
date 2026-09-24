using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Products.Queries.SearchProducts;

public sealed record SearchProductsQuery(ProductSearchRequest Request, bool AdminView = false) : IQuery<Result<PagedResponse<ProductListItemResponse>>>;
