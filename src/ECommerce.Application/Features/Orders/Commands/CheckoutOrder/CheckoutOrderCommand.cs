using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Orders.Commands.CheckoutOrder;

public sealed record CheckoutOrderCommand(string CustomerId, CreateOrderRequest Request) : ICommand<Result<OrderResponse>>;
