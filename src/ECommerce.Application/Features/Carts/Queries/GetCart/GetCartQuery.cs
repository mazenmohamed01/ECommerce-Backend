using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Carts.Queries.GetCart;

public sealed record GetCartQuery(string CustomerId) : IQuery<Result<CartResponse>>;
