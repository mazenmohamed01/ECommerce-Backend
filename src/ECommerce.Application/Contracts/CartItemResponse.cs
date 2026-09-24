namespace ECommerce.Application.Contracts;

public sealed record CartItemResponse
{
    public Guid CartItemId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = default!;
    public string ProductSlug { get; init; } = default!;
    public string? MainImageUrl { get; init; }
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
    public bool IsAvailable { get; init; }
    
    /// <summary>
    /// Populated only if IsAvailable = false (e.g. stock dropped below requested quantity).
    /// </summary>
    public int? AvailableStock { get; init; }
}
