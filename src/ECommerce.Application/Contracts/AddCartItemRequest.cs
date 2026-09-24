namespace ECommerce.Application.Contracts;

public sealed record AddCartItemRequest(
    Guid ProductId,
    int Quantity
);
