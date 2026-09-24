namespace ECommerce.Application.Contracts;

/// <summary>Request body for POST /api/admin/products.</summary>
public sealed record CreateProductRequest
{
    /// <summary>Category this product belongs to. Must be an active category.</summary>
    public Guid     CategoryId      { get; init; }

    /// <summary>Product display name. Required, max 200 chars.</summary>
    public string   Name            { get; init; } = default!;

    /// <summary>
    /// URL slug. Optional — auto-generated from Name when absent.
    /// When provided must match ^[a-z0-9]+(?:-[a-z0-9]+)*$
    /// </summary>
    public string?  Slug            { get; init; }

    /// <summary>Stock-keeping unit code. Required, max 50 chars, must be unique.</summary>
    public string   Sku             { get; init; } = default!;

    /// <summary>Long-form product description. Optional.</summary>
    public string?  Description     { get; init; }

    /// <summary>Selling price. Must be greater than 0.</summary>
    public decimal  Price           { get; init; }

    /// <summary>Current stock quantity. Must be >= 0.</summary>
    public int      QuantityInStock { get; init; }
}
