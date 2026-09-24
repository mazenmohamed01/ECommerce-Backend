using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByIdAsync(Guid id, bool includeItems = true, CancellationToken cancellationToken = default);
    
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    
    Task<(IEnumerable<Order> Items, int TotalCount)> GetByCustomerIdAsync(
        string customerId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default);
    
    Task<(IEnumerable<Order> Items, int TotalCount)> SearchForAdminAsync(
        OrderSearchFilter filter, 
        CancellationToken cancellationToken = default);
    
    Task AddStatusHistoryAsync(OrderStatusHistory entry, CancellationToken cancellationToken = default);
    
    Task<List<Order>> GetExpiredPendingOnlineOrdersAsync(DateTime cutoff, CancellationToken cancellationToken = default);
    
    Task<Order?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default);
    
    Task UpdatePaymentDetailsAsync(Guid orderId, string paymentId, string paymentGateway, CancellationToken cancellationToken = default);
}

public sealed class OrderSearchFilter
{
    public OrderStatus? Status { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Keyword { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string SortBy { get; init; } = "CreatedAt";
    public string SortDirection { get; init; } = "desc";
}
