namespace ECommerce.Application.Contracts;

/// <summary>
/// Full product detail response returned by GET /api/products/{slug} and Admin detail endpoint.
/// Includes all list fields plus description, SKU, stock, and the full image gallery.
/// </summary>
public sealed record ProductDetailResponse
{
    public Guid    Id              { get; init; }
    public Guid    CategoryId      { get; init; }
    public string  CategoryName    { get; init; } = default!;
    public string  Name            { get; init; } = default!;
    public string  Slug            { get; init; } = default!;
    public string  Sku             { get; init; } = default!;
    public string? Description     { get; init; }
    public decimal Price           { get; init; }
    public int     QuantityInStock { get; init; }
    public bool    IsActive        { get; init; }

    /// <summary>Computed: true when QuantityInStock == 0.</summary>
    public bool    IsOutOfStock    { get; init; }

    public string? MainImageUrl    { get; init; }

    /// <summary>Full ordered image gallery for this product.</summary>
    public IReadOnlyList<ProductImageResponse> Images { get; init; } = [];

    public DateTime  CreatedAt     { get; init; }
    public DateTime? UpdatedAt     { get; init; }
    public DateTime? DeletedAt     { get; init; }
}
