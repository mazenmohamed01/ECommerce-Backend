namespace ECommerce.Application.Contracts;

public sealed record CartResponse
{
    public Guid CartId { get; init; }
    public IReadOnlyList<CartItemResponse> Items { get; init; } = [];
    public decimal SubTotal { get; init; }
    public int TotalItemsCount { get; init; }
    public bool HasUnavailableItems { get; init; }
}
