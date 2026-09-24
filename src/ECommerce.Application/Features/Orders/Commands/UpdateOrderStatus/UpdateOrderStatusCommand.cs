using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using System;

namespace ECommerce.Application.Features.Orders.Commands.UpdateOrderStatus;

public sealed record UpdateOrderStatusCommand(Guid OrderId, UpdateOrderStatusRequest Request, string ChangedByAdminId) : ICommand<Result<OrderResponse>>;
