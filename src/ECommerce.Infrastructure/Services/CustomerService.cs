using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Common;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;

    public CustomerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResponse<CustomerResponse>>> SearchCustomersAsync(
        string? searchTerm, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(o => 
                o.CustomerName.ToLower().Contains(searchTerm) || 
                o.CustomerEmail.ToLower().Contains(searchTerm) ||
                o.CustomerPhone.ToLower().Contains(searchTerm));
        }

        var customerGroups = query.GroupBy(o => o.CustomerId);

        var totalItems = await customerGroups.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var customerStatsQuery = customerGroups
            .Select(g => new
            {
                CustomerId = g.Key,
                CustomerName = g.OrderByDescending(o => o.CreatedAt).Select(o => o.CustomerName).FirstOrDefault() ?? "Unknown",
                CustomerEmail = g.OrderByDescending(o => o.CreatedAt).Select(o => o.CustomerEmail).FirstOrDefault() ?? "Unknown",
                CustomerPhone = g.OrderByDescending(o => o.CreatedAt).Select(o => o.CustomerPhone).FirstOrDefault() ?? "Unknown",
                TotalOrders = g.Count(),
                TotalSpent = g.Sum(o => o.TotalPrice),
                CreatedAt = g.Min(o => o.CreatedAt)
            });

        var customersData = await customerStatsQuery
            .OrderByDescending(c => c.TotalOrders)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var customerIds = customersData.Select(c => c.CustomerId).ToList();
        var users = await _context.Users
            .Where(u => customerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u, cancellationToken);

        var customers = customersData.Select(c => 
        {
            users.TryGetValue(c.CustomerId, out var user);
            var name = user != null && !string.IsNullOrWhiteSpace(user.FirstName) 
                ? $"{user.FirstName} {user.LastName}".Trim()
                : c.CustomerName;
            var email = user?.Email ?? c.CustomerEmail;
            var phone = user?.PhoneNumber ?? c.CustomerPhone;

            return new CustomerResponse(
                c.CustomerId,
                name,
                email,
                phone,
                c.TotalOrders,
                c.TotalSpent,
                c.CreatedAt
            );
        }).ToList();

        var pagedList = new PagedResponse<CustomerResponse>
        {
            Items = customers,
            TotalCount = totalItems,
            PageNumber = page,
            PageSize = pageSize
        };

        return Result.Success(pagedList);
    }

    public async Task<Result<CustomerDetailsResponse>> GetCustomerDetailsAsync(
        string customerId, 
        CancellationToken cancellationToken = default)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId && o.OrderStatus != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        if (!orders.Any())
        {
            return Result.Failure<CustomerDetailsResponse>(
                Error.NotFound("CUSTOMER_NOT_FOUND", "No customer found with the given ID"));
        }

        var latestOrder = orders.First();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == customerId, cancellationToken);
        
        var name = user != null && !string.IsNullOrWhiteSpace(user.FirstName) 
            ? $"{user.FirstName} {user.LastName}".Trim()
            : latestOrder.CustomerName;
        var email = user?.Email ?? latestOrder.CustomerEmail;
        var phone = user?.PhoneNumber ?? latestOrder.CustomerPhone;

        var orderSummaries = orders.Select(o => new OrderSummaryResponse
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = o.CustomerName,
            OrderStatus = o.OrderStatus.ToString(),
            PaymentMethod = o.PaymentMethod.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            TotalPrice = o.TotalPrice,
            CreatedAt = o.CreatedAt
        }).ToList();

        var response = new CustomerDetailsResponse(
            customerId,
            name,
            email,
            phone,
            orders.Count,
            orders.Sum(o => o.TotalPrice),
            orders.Last().CreatedAt,
            orderSummaries
        );

        return Result.Success(response);
    }
}
