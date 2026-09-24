namespace ECommerce.Application.Contracts;

/// <summary>Request body for PUT /api/admin/categories/{id}.</summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>Updated display name. Required.</summary>
    public string  Name        { get; init; } = default!;

    /// <summary>Updated URL slug. Optional — if omitted, auto-generated from Name.</summary>
    public string? Slug        { get; init; }

    /// <summary>Updated description. Optional.</summary>
    public string? Description { get; init; }

    /// <summary>Updated sort position.</summary>
    public int     SortOrder   { get; init; } = 0;

    /// <summary>Whether the category is publicly visible. Defaults to true.</summary>
    public bool    IsActive    { get; init; } = true;
}
