namespace ECommerce.Application.Contracts;

public sealed record DashboardStatsResponse(
    decimal TotalRevenue,
    decimal RevenueTrend,
    int TotalOrders,
    decimal OrdersTrend,
    int TotalProducts,
    int OutOfStockProducts,
    int TotalCustomers,
    decimal CustomersTrend,
    List<DailySalesResponse> SalesChartData,
    List<DashboardRecentOrderResponse> RecentOrders
);

public sealed record DailySalesResponse(
    string Name,
    decimal Total
);

public sealed record DashboardRecentOrderResponse(
    string Id,
    string OrderNumber,
    string CustomerName,
    decimal Amount,
    string Status,
    string ColorClass
);
