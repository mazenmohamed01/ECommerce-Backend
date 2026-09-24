using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;
using ECommerce.Domain.ValueObjects;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Order aggregate root — owns OrderItems.
/// All pricing fields are set at creation and treated as immutable snapshots.
/// Payment fields are set later by the payment callback handler.
/// </summary>
public sealed class Order : AggregateRoot
{
    // ── Identity ──────────────────────────────────────────────────────────────
    /// <summary>Human-readable reference shown to the customer. Format: ORD24100001</summary>
    public string OrderNumber { get; private set; } = default!;

    // ── Customer Snapshot ─────────────────────────────────────────────────────
    public string CustomerId    { get; private set; } = default!;
    public string CustomerName  { get; private set; } = default!;
    public string CustomerPhone { get; private set; } = default!;
    public string CustomerEmail { get; private set; } = default!;

    // ── Shipping Address (Owned Entity) ───────────────────────────────────────
    public ShippingAddress ShippingAddress { get; private set; } = default!;

    public string? Notes { get; private set; }

    // ── Pricing ───────────────────────────────────────────────────────────────
    public decimal SubTotal       { get; private set; }
    public decimal ShippingCost   { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalPrice     { get; private set; }

    // ── Payment ───────────────────────────────────────────────────────────────
    public PaymentStatus PaymentStatus  { get; private set; } = PaymentStatus.Pending;
    public PaymentMethod PaymentMethod  { get; private set; }
    public string?       PaymentGateway { get; private set; }   // "Moyasar", "HyperPay"
    public string?       PaymentId      { get; private set; }   // gateway payment ID
    public string?       TransactionId  { get; private set; }   // gateway transaction ID
    public DateTime?     PaidAt         { get; private set; }

    // ── Status ────────────────────────────────────────────────────────────────
    public OrderStatus OrderStatus { get; private set; } = OrderStatus.Pending;

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<OrderStatusHistory> _statusHistory = [];
    public IReadOnlyCollection<OrderStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Order() { }  // EF Core

    public static Order Create(
        string          orderNumber,
        string          customerId,
        string          customerName,
        string          customerPhone,
        string          customerEmail,
        ShippingAddress shippingAddress,
        PaymentMethod   paymentMethod,
        decimal         subTotal,
        decimal         shippingCost,
        decimal         discountAmount,
        string?         notes = null)
    {
        return new Order
        {
            OrderNumber     = orderNumber,
            CustomerId      = customerId,
            CustomerName    = customerName,
            CustomerPhone   = customerPhone,
            CustomerEmail   = customerEmail,
            ShippingAddress = shippingAddress,
            PaymentMethod   = paymentMethod,
            SubTotal        = subTotal,
            ShippingCost    = shippingCost,
            DiscountAmount  = discountAmount,
            TotalPrice      = subTotal + shippingCost - discountAmount,
            Notes           = notes
        };
    }

    // ── Domain Methods ────────────────────────────────────────────────────────

    public void AddItem(OrderItem item)
    {
        _items.Add(item);
        SetUpdatedAt();
    }

    public void MarkAsPaid(
        string   paymentGateway,
        string   paymentId,
        string   transactionId)
    {
        PaymentStatus  = PaymentStatus.Paid;
        PaymentGateway = paymentGateway;
        PaymentId      = paymentId;
        TransactionId  = transactionId;
        PaidAt         = DateTime.UtcNow;
        OrderStatus    = OrderStatus.Confirmed;
        SetUpdatedAt();
    }

    /// <summary>Called when the payment gateway reports failure.</summary>
    public void MarkPaymentFailed()
    {
        PaymentStatus = PaymentStatus.Failed;
        OrderStatus   = OrderStatus.Cancelled;
        SetUpdatedAt();
    }

    public void UpdateStatus(OrderStatus status, string changedByUserId, string? reason = null)
    {
        var history = OrderStatusHistory.Create(Id, OrderStatus, status, changedByUserId, reason);
        _statusHistory.Add(history);

        OrderStatus = status;
        SetUpdatedAt();
    }

    public void Cancel(string changedByUserId, string? reason = null)
    {
        var history = OrderStatusHistory.Create(Id, OrderStatus, OrderStatus.Cancelled, changedByUserId, reason);
        _statusHistory.Add(history);

        OrderStatus   = OrderStatus.Cancelled;
        PaymentStatus = PaymentStatus.Pending == PaymentStatus
            ? PaymentStatus.Cancelled
            : PaymentStatus;
        SetUpdatedAt();
    }
}
