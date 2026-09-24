namespace ECommerce.Application.Contracts;

/// <summary>
/// Lightweight product response used in paginated list results (GET /api/products).
/// Omits heavy fields like Description and image list to keep payload small.
/// </summary>
public sealed record ProductListItemResponse
{
    public Guid    Id           { get; init; }
    public string  Name         { get; init; } = default!;
    public string  Slug         { get; init; } = default!;
    public decimal Price        { get; init; }

    /// <summary>URL of the image marked as IsMain. Null if the product has no images.</summary>
    public string? MainImageUrl { get; init; }

    /// <summary>Computed field: true when QuantityInStock == 0. Product remains visible.</summary>
    public bool    IsOutOfStock { get; init; }

    public string  CategoryName { get; init; } = default!;
    public Guid    CategoryId   { get; init; }
}
