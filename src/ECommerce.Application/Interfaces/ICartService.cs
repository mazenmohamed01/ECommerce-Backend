using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Business logic contract for Cart operations.
/// </summary>
public interface ICartService
{
    Task<Result<CartResponse>> GetCartAsync(string customerId, CancellationToken cancellationToken = default);
    
    Task<Result<CartResponse>> AddItemAsync(string customerId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    
    Task<Result<CartResponse>> UpdateItemQuantityAsync(string customerId, Guid cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    
    Task<Result<CartResponse>> RemoveItemAsync(string customerId, Guid cartItemId, CancellationToken cancellationToken = default);
    
    Task<Result> ClearCartAsync(string customerId, CancellationToken cancellationToken = default);
}
