namespace ECommerce.Application.Contracts;

/// <summary>API response representing a single category.</summary>
public sealed record CategoryResponse
{
    public Guid     Id           { get; init; }
    public string   Name         { get; init; } = default!;
    public string   Slug         { get; init; } = default!;
    public string?  Description  { get; init; }
    public string?  ImageUrl     { get; init; }
    public int      SortOrder    { get; init; }
    public bool     IsActive     { get; init; }

    /// <summary>Count of products belonging to this category (including inactive products).</summary>
    public int      ProductCount { get; init; }

    public DateTime CreatedAt    { get; init; }
    public DateTime? UpdatedAt   { get; init; }
}
