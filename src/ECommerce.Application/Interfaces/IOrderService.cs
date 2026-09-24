using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

public interface IOrderService
{
    /// <summary>
    /// Executes the checkout process: validates cart, reserves stock, creates the order, and clears the cart.
    /// </summary>
    Task<Result<OrderResponse>> CheckoutAsync(
        string customerId, 
        CreateOrderRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific order. If customerId is provided, enforces that the order belongs to that customer.
    /// </summary>
    Task<Result<OrderResponse>> GetByIdAsync(
        Guid id, 
        string? customerId = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves paginated list of orders for a specific customer.
    /// </summary>
    Task<Result<PagedResponse<OrderSummaryResponse>>> GetCustomerOrdersAsync(
        string customerId, 
        PaginationRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin search for orders with complex filtering.
    /// </summary>
    Task<Result<PagedResponse<OrderSummaryResponse>>> AdminSearchOrdersAsync(
        AdminOrderSearchRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin manual update of order status (e.g., Shipping, Delivered, Cancelled).
    /// </summary>
    Task<Result<OrderResponse>> UpdateOrderStatusAsync(
        Guid id, 
        UpdateOrderStatusRequest request, 
        string changedByAdminId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a payment process for an online order.
    /// </summary>
    Task<Result<PaymentInitiationResponse>> InitiatePaymentAsync(
        Guid orderId, 
        string customerId, 
        CancellationToken cancellationToken = default);
}
