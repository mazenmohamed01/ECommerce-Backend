using ECommerce.Domain.Enums;

namespace ECommerce.Application.Contracts;

public sealed record CreateOrderRequest(
    string CustomerName,
    string CustomerPhone,
    string Street,
    string District,
    string City,
    string State,
    string PostalCode,
    PaymentMethod PaymentMethod,
    string? Notes = null
);
