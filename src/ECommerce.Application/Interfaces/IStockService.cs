using ECommerce.Application.Common;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Domain service for handling stock reservation, release, and confirmation.
/// Operates strictly within transactions using database row-level locking.
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Locks the product row and safely increments ReservedStock by the requested quantity.
    /// Returns a failure Result if AvailableStock (QuantityInStock - ReservedStock) is insufficient.
    /// </summary>
    Task<Result> ReserveStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the product row and safely decrements ReservedStock by the requested quantity.
    /// Used when an order is cancelled or times out.
    /// </summary>
    Task<Result> ReleaseStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically deducts both QuantityInStock and ReservedStock by the requested quantity.
    /// Used when a payment is successful and stock is permanently consumed.
    /// </summary>
    Task<Result> ConfirmStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores stock that was previously deducted. 
    /// Used when an already confirmed order (e.g. paid online) is cancelled.
    /// </summary>
    Task<Result> RestoreStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
