using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

/// <summary>
/// Cart-specific repository contract.
/// </summary>
public interface ICartRepository : IRepository<Cart>
{
    /// <summary>
    /// Retrieves a Cart by the Customer's User Id.
    /// </summary>
    Task<Cart?> GetByCustomerIdAsync(string customerId, bool includeItems = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific item within a cart.
    /// </summary>
    Task<CartItem?> GetItemAsync(Guid cartId, Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a line item to the database.
    /// </summary>
    Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a line item from the database.
    /// </summary>
    Task RemoveItemAsync(Guid cartItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all items from a given cart.
    /// </summary>
    Task ClearItemsAsync(Guid cartId, CancellationToken cancellationToken = default);
}
