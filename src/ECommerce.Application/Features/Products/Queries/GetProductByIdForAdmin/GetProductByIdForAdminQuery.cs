using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Products.Queries.GetProductByIdForAdmin;

public sealed record GetProductByIdForAdminQuery(Guid Id) : IQuery<Result<ProductDetailResponse>>;
