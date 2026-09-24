using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetByIdAsync(Guid id, bool includeItems = true, CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (includeItems)
        {
            query = query
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.StatusHistory);
        }

        return await query.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetByCustomerIdAsync(
        string customerId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(o => o.CustomerId == customerId);
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        var safePageSize = Math.Min(Math.Max(pageSize, 1), 100);
        var safePage = Math.Max(page, 1);

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> SearchForAdminAsync(
        OrderSearchFilter filter, 
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(o => o.OrderStatus == filter.Status.Value);

        if (filter.PaymentMethod.HasValue)
            query = query.Where(o => o.PaymentMethod == filter.PaymentMethod.Value);

        if (filter.DateFrom.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = $"%{filter.Keyword}%";
            query = query.Where(o => 
                EF.Functions.ILike(o.OrderNumber, kw) ||
                EF.Functions.ILike(o.CustomerName, kw) ||
                EF.Functions.ILike(o.CustomerPhone, kw));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = filter.SortBy.ToLowerInvariant() switch
        {
            "totalprice" => filter.SortDirection.ToLowerInvariant() == "asc" 
                ? query.OrderBy(o => o.TotalPrice) 
                : query.OrderByDescending(o => o.TotalPrice),
            _ => filter.SortDirection.ToLowerInvariant() == "asc" 
                ? query.OrderBy(o => o.CreatedAt) 
                : query.OrderByDescending(o => o.CreatedAt)
        };

        var safePageSize = Math.Min(Math.Max(filter.PageSize, 1), 100);
        var safePage = Math.Max(filter.Page, 1);

        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddStatusHistoryAsync(OrderStatusHistory entry, CancellationToken cancellationToken = default)
    {
        await Context.OrderStatusHistories.AddAsync(entry, cancellationToken);
    }

    public async Task<List<Order>> GetExpiredPendingOnlineOrdersAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        // Uses the composite index: (OrderStatus, PaymentMethod, CreatedAt)
        return await DbSet
            .Where(o => o.PaymentMethod == PaymentMethod.Online 
                     && o.OrderStatus == OrderStatus.Pending 
                     && o.CreatedAt < cutoff)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByPaymentIdAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.PaymentId == paymentId, cancellationToken);
    }

    public async Task UpdatePaymentDetailsAsync(Guid orderId, string paymentId, string paymentGateway, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.PaymentId, paymentId)
                .SetProperty(o => o.PaymentGateway, paymentGateway), 
                cancellationToken);
    }
}
