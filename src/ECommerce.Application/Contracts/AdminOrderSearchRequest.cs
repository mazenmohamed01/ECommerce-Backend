using ECommerce.Domain.Enums;

namespace ECommerce.Application.Contracts;

public sealed record AdminOrderSearchRequest : PaginationRequest
{
    public OrderStatus? Status { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Keyword { get; init; }
    public string SortBy { get; init; } = "CreatedAt";
    public string SortDirection { get; init; } = "desc";
}
