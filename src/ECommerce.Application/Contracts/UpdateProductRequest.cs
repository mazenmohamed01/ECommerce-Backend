namespace ECommerce.Application.Contracts;

/// <summary>Request body for PUT /api/admin/products/{id}.</summary>
public sealed record UpdateProductRequest
{
    /// <summary>Updated category. Must be an active category.</summary>
    public Guid     CategoryId      { get; init; }

    /// <summary>Updated product name. Required.</summary>
    public string   Name            { get; init; } = default!;

    /// <summary>Updated URL slug. Optional — auto-generated from Name when absent.</summary>
    public string?  Slug            { get; init; }

    /// <summary>Updated SKU code. Required.</summary>
    public string   Sku             { get; init; } = default!;

    /// <summary>Updated description. Optional.</summary>
    public string?  Description     { get; init; }

    /// <summary>Updated price. Must be greater than 0.</summary>
    public decimal  Price           { get; init; }

    /// <summary>Updated stock quantity. Must be >= 0.</summary>
    public int      QuantityInStock { get; init; }

    /// <summary>Whether the product is publicly visible. Defaults to true.</summary>
    public bool     IsActive        { get; init; } = true;
}
