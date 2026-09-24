namespace ECommerce.Application.Contracts;

public sealed record CustomerResponse(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    int TotalOrders,
    decimal TotalSpent,
    DateTime CreatedAt
);
