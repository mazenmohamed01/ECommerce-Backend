using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class OrderStatusHistory : BaseEntity
{
    public Guid OrderId { get; private set; }
    public OrderStatus PreviousStatus { get; private set; }
    public OrderStatus NewStatus { get; private set; }
    public string ChangedByUserId { get; private set; } = default!;
    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;
    public string? Reason { get; private set; }

    private OrderStatusHistory() { } // EF Core

    public static OrderStatusHistory Create(
        Guid orderId,
        OrderStatus previousStatus,
        OrderStatus newStatus,
        string changedByUserId,
        string? reason = null)
    {
        return new OrderStatusHistory
        {
            OrderId = orderId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow,
            Reason = reason
        };
    }
}
