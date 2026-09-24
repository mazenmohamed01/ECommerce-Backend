using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Represents a single line item in a shopping cart.
/// </summary>
public sealed class CartItem : BaseEntity
{
    public Guid CartId    { get; private set; }
    public Guid ProductId { get; private set; }
    public int  Quantity  { get; private set; }

    // Navigation properties
    public Cart?    Cart    { get; private set; }
    public Product? Product { get; private set; }

    private CartItem() { } // EF Core

    private CartItem(Guid cartId, Guid productId, int quantity)
    {
        CartId    = cartId;
        ProductId = productId;
        Quantity  = quantity;
    }

    internal static CartItem Create(Guid cartId, Guid productId, int quantity) 
        => new(cartId, productId, quantity);

    internal void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
        SetUpdatedAt();
    }
}
