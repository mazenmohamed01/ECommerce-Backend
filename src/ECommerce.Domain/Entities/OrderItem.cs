using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Line item inside an Order aggregate.
/// All product fields are snapshots — frozen at order time.
/// </summary>
public sealed class OrderItem : BaseEntity
{
    public Guid    OrderId     { get; private set; }
    public Guid    ProductId   { get; private set; }

    // Snapshot fields — taken from the product at order creation time
    public string  ProductName { get; private set; } = default!;
    public string  ProductSku  { get; private set; } = default!;
    public decimal UnitPrice   { get; private set; }
    public int     Quantity    { get; private set; }
    public decimal TotalPrice  { get; private set; }  // UnitPrice * Quantity

    public Order   Order   { get; private set; } = default!;
    public Product Product { get; private set; } = default!;

    private OrderItem() { }  // EF Core

    public static OrderItem Create(
        Guid    orderId,
        Guid    productId,
        string  productName,
        string  productSku,
        decimal unitPrice,
        int     quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        return new OrderItem
        {
            OrderId     = orderId,
            ProductId   = productId,
            ProductName = productName,
            ProductSku  = productSku,
            UnitPrice   = unitPrice,
            Quantity    = quantity,
            TotalPrice  = unitPrice * quantity
        };
    }
}
