namespace ECommerce.Application.Contracts;

/// <summary>Request body for creating a new category.</summary>
public sealed record CreateCategoryRequest
{
    /// <summary>Display name of the category. Required.</summary>
    public string  Name        { get; init; } = default!;

    /// <summary>
    /// URL-friendly slug. Optional — if omitted, auto-generated from Name.
    /// When provided must match ^[a-z0-9]+(?:-[a-z0-9]+)*$
    /// </summary>
    public string? Slug        { get; init; }

    /// <summary>Short marketing description. Optional, max 500 chars.</summary>
    public string? Description { get; init; }

    /// <summary>Sort position for ordering category lists. Defaults to 0.</summary>
    public int     SortOrder   { get; init; } = 0;
}
