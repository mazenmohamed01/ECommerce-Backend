using ECommerce.Domain.Enums;

namespace ECommerce.Application.Contracts;

public sealed record UpdateOrderStatusRequest(
    OrderStatus NewStatus,
    string? Reason = null
);
