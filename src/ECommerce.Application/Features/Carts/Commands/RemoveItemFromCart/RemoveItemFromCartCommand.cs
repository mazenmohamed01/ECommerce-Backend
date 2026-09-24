using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Carts.Commands.RemoveItemFromCart;

public sealed record RemoveItemFromCartCommand(string CustomerId, Guid CartItemId) : ICommand<Result<CartResponse>>;
