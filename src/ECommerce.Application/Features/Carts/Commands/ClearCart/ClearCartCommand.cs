using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;

namespace ECommerce.Application.Features.Carts.Commands.ClearCart;

public sealed record ClearCartCommand(string CustomerId) : ICommand<Result>;
