using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Product aggregate root.
/// Restricts delete when it has been ordered — use IsActive = false to archive.
/// </summary>
public sealed class Product : AggregateRoot
{
    public Guid    CategoryId      { get; private set; }
    public string  Name            { get; private set; } = default!;
    public string  Slug            { get; private set; } = default!;
    public string  Sku             { get; private set; } = default!;
    public string? Description     { get; private set; }
    public decimal Price           { get; private set; }
    public int     QuantityInStock { get; private set; }
    public int     ReservedStock   { get; private set; } = 0;
    public bool      IsActive        { get; private set; } = true;
    public DateTime? DeletedAt       { get; private set; }

    public int AvailableStock => QuantityInStock - ReservedStock;

    public Category Category { get; private set; } = default!;

    private readonly List<ProductImage> _images = [];
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    private Product() { }  // EF Core

    public static Product Create(
        Guid    categoryId,
        string  name,
        string  slug,
        string  sku,
        decimal price,
        int     quantityInStock,
        string? description = null)
    {
        return new Product
        {
            CategoryId      = categoryId,
            Name            = name,
            Slug            = slug,
            Sku             = sku,
            Price           = price,
            QuantityInStock = quantityInStock,
            Description     = description
        };
    }

    public void Update(
        Guid    categoryId,
        string  name,
        string  slug,
        string  sku,
        decimal price,
        int     quantityInStock,
        string? description = null,
        bool    isActive    = true)
    {
        CategoryId      = categoryId;
        Name            = name;
        Slug            = slug;
        Sku             = sku;
        Price           = price;
        QuantityInStock = quantityInStock;
        Description     = description;
        IsActive        = isActive;
        SetUpdatedAt();
    }

    /// <summary>
    /// Decrements stock after an order is confirmed.
    /// Throws if insufficient stock.
    /// </summary>
    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        if (QuantityInStock < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for product '{Name}'. Available: {QuantityInStock}, Requested: {quantity}.");

        QuantityInStock -= quantity;
        SetUpdatedAt();
    }

    /// <summary>Restores stock when an order is cancelled.</summary>
    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        QuantityInStock += quantity;
        SetUpdatedAt();
    }

    /// <summary>Reserves stock for online orders before payment.</summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        if (AvailableStock < quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for product '{Name}'. Available: {AvailableStock}, Requested: {quantity}.");

        ReservedStock += quantity;
        SetUpdatedAt();
    }

    /// <summary>Releases reserved stock (e.g. if online order expires).</summary>
    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));

        if (ReservedStock < quantity)
            throw new InvalidOperationException($"Cannot release {quantity} reserved stock, only {ReservedStock} is reserved.");

        ReservedStock -= quantity;
        SetUpdatedAt();
    }

    /// <summary>
    /// Soft-deletes the product: marks inactive and records deletion timestamp.
    /// Used by the Admin delete endpoint — no physical row is ever removed.
    /// </summary>
    public void SoftDelete()
    {
        IsActive  = false;
        DeletedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive  = false;
        DeletedAt = DateTime.UtcNow;
        SetUpdatedAt();
    }
}
