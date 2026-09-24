using System.Globalization;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DashboardStatsResponse>> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

        // 1. Total Revenue
        var currentRevenue = await _context.Orders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.CreatedAt >= thirtyDaysAgo)
            .SumAsync(o => o.TotalPrice, cancellationToken);
            
        var previousRevenue = await _context.Orders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.CreatedAt >= sixtyDaysAgo && o.CreatedAt < thirtyDaysAgo)
            .SumAsync(o => o.TotalPrice, cancellationToken);
            
        var allTimeRevenue = await _context.Orders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalPrice, cancellationToken);
            
        var revenueTrend = CalculateTrend(currentRevenue, previousRevenue);

        // 2. Total Orders
        var currentOrders = await _context.Orders
            .CountAsync(o => o.OrderStatus != OrderStatus.Cancelled && o.CreatedAt >= thirtyDaysAgo, cancellationToken);
            
        var previousOrders = await _context.Orders
            .CountAsync(o => o.OrderStatus != OrderStatus.Cancelled && o.CreatedAt >= sixtyDaysAgo && o.CreatedAt < thirtyDaysAgo, cancellationToken);
            
        var allTimeOrders = await _context.Orders
            .CountAsync(o => o.OrderStatus != OrderStatus.Cancelled, cancellationToken);
            
        var ordersTrend = CalculateTrend(currentOrders, previousOrders);

        // 3. Products
        var totalProducts = await _context.Products.CountAsync(cancellationToken);
        var outOfStockProducts = await _context.Products.CountAsync(p => p.QuantityInStock == 0, cancellationToken);

        // 4. Customers
        var currentCustomers = await _context.Users
            .CountAsync(u => u.CreatedAt >= thirtyDaysAgo, cancellationToken);
            
        var previousCustomers = await _context.Users
            .CountAsync(u => u.CreatedAt >= sixtyDaysAgo && u.CreatedAt < thirtyDaysAgo, cancellationToken);
            
        var allTimeCustomers = await _context.Users.CountAsync(cancellationToken);
        
        var customersTrend = CalculateTrend(currentCustomers, previousCustomers);

        // 5. Sales Chart (Last 7 Days)
        var sevenDaysAgo = now.Date.AddDays(-6);
        
        var recentSales = await _context.Orders
            .Where(o => o.OrderStatus != OrderStatus.Cancelled && o.CreatedAt >= sevenDaysAgo)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalPrice) })
            .ToListAsync(cancellationToken);

        var arCulture = new CultureInfo("ar-SA");
        var chartData = new List<DailySalesResponse>();
        for (int i = 0; i < 7; i++)
        {
            var date = sevenDaysAgo.AddDays(i);
            var sale = recentSales.FirstOrDefault(s => s.Date == date);
            var dayName = date.ToString("dddd", arCulture);
            chartData.Add(new DailySalesResponse(dayName, sale?.Total ?? 0));
        }

        // 6. Recent Orders
        var recentOrdersList = await _context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new DashboardRecentOrderResponse(
                o.Id.ToString(),
                o.OrderNumber,
                o.CustomerName,
                o.TotalPrice,
                GetStatusText(o.OrderStatus),
                GetStatusColor(o.OrderStatus)
            ))
            .ToListAsync(cancellationToken);

        return Result.Success(new DashboardStatsResponse(
            allTimeRevenue,
            revenueTrend,
            allTimeOrders,
            ordersTrend,
            totalProducts,
            outOfStockProducts,
            allTimeCustomers,
            customersTrend,
            chartData,
            recentOrdersList
        ));
    }

    private static decimal CalculateTrend(decimal current, decimal previous)
    {
        if (previous == 0) return current > 0 ? 100 : 0;
        return ((current - previous) / previous) * 100;
    }

    private static string GetStatusText(OrderStatus status) => status switch
    {
        OrderStatus.Delivered => "مكتمل",
        OrderStatus.Processing => "قيد التجهيز",
        OrderStatus.Confirmed => "مؤكد",
        OrderStatus.Shipped => "تم الشحن",
        OrderStatus.Pending => "بانتظار التأكيد",
        OrderStatus.Cancelled => "ملغي",
        _ => status.ToString()
    };

    private static string GetStatusColor(OrderStatus status) => status switch
    {
        OrderStatus.Delivered => "bg-emerald-500/10 text-emerald-500",
        OrderStatus.Processing => "bg-amber-500/10 text-amber-500",
        OrderStatus.Confirmed => "bg-indigo-500/10 text-indigo-500",
        OrderStatus.Shipped => "bg-blue-500/10 text-blue-500",
        OrderStatus.Pending => "bg-slate-500/20 text-slate-300",
        OrderStatus.Cancelled => "bg-red-500/10 text-red-500",
        _ => "bg-secondary text-muted-foreground"
    };
}
