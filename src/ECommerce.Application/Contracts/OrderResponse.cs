using ECommerce.Domain.Enums;

namespace ECommerce.Application.Contracts;

public sealed record OrderResponse
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public decimal SubTotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalPrice { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public List<OrderItemResponse> Items { get; init; } = new();
    public List<OrderStatusHistoryResponse> StatusHistory { get; init; } = new();
}

public sealed record OrderItemResponse
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
    public string ProductSlug { get; init; } = string.Empty;
    public string MainImageUrl { get; init; } = string.Empty;
}

public sealed record OrderStatusHistoryResponse
{
    public string PreviousStatus { get; init; } = string.Empty;
    public string NewStatus { get; init; } = string.Empty;
    public string ChangedByUserId { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string? Reason { get; init; }
}
