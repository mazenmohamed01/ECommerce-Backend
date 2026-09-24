using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Carts.Commands.UpdateCartItemQuantity;

public sealed record UpdateCartItemQuantityCommand(string CustomerId, Guid CartItemId, UpdateCartItemRequest Request) : ICommand<Result<CartResponse>>;
