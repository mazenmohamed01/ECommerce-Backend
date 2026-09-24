namespace ECommerce.Application.Contracts;

public sealed record CustomerDetailsResponse(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    int TotalOrders,
    decimal TotalSpent,
    DateTime CreatedAt,
    IReadOnlyList<OrderSummaryResponse> Orders
);
