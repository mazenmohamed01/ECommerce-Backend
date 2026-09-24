using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Carts.Commands.AddItemToCart;

public sealed record AddItemToCartCommand(string CustomerId, AddCartItemRequest Request) : ICommand<Result<CartResponse>>;
