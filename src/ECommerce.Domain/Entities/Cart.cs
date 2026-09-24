using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Represents a customer's shopping cart.
/// Enforces business rules regarding item management.
/// </summary>
public sealed class Cart : BaseEntity
{
    public string CustomerId { get; private set; } = default!;

    // Navigation properties
    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    private Cart() { } // EF Core

    private Cart(string customerId)
    {
        CustomerId = customerId;
    }

    public static Cart Create(string customerId) => new(customerId);

    /// <summary>
    /// Adds a product to the cart or increments its quantity if it already exists.
    /// Does NOT validate stock — the CartService validates stock before calling this.
    /// </summary>
    public void AddItem(Guid productId, int quantity)
    {
        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem is null)
        {
            _items.Add(CartItem.Create(Id, productId, quantity));
        }
        else
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        SetUpdatedAt();
    }

    /// <summary>
    /// Updates the quantity of an existing item.
    /// Removing items entirely should be done via RemoveItem.
    /// </summary>
    public void UpdateItemQuantity(Guid cartItemId, int quantity)
    {
        var existingItem = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (existingItem is not null)
        {
            existingItem.UpdateQuantity(quantity);
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Removes a specific item from the cart.
    /// </summary>
    public void RemoveItem(Guid cartItemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == cartItemId);
        if (item is not null)
        {
            _items.Remove(item);
            SetUpdatedAt();
        }
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    public void ClearItems()
    {
        if (_items.Count > 0)
        {
            _items.Clear();
            SetUpdatedAt();
        }
    }
}
