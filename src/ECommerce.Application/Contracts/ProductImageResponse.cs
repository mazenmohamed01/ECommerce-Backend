namespace ECommerce.Application.Contracts;

/// <summary>Represents a single product image in API responses.</summary>
public sealed record ProductImageResponse
{
    public Guid    Id           { get; init; }
    public string  ImageUrl     { get; init; } = default!;
    public string? AltText      { get; init; }
    public int     DisplayOrder { get; init; }
    public bool    IsMain       { get; init; }
}
