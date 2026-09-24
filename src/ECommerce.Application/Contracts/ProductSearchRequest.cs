namespace ECommerce.Application.Contracts;

/// <summary>
/// Query parameters for GET /api/products.
/// All fields are optional — omitted parameters are ignored in filtering/sorting.
/// </summary>
public sealed record ProductSearchRequest
{
    /// <summary>Case-insensitive partial match against Name and Description.</summary>
    public string?  Keyword       { get; init; }

    /// <summary>Filter by exact category ID.</summary>
    public Guid?    CategoryId    { get; init; }

    /// <summary>Filter by exact category Slug (e.g. 'electronics').</summary>
    public string?  Category      { get; init; }

    /// <summary>Minimum price (inclusive).</summary>
    public decimal? PriceFrom     { get; init; }

    /// <summary>Maximum price (inclusive). Must be >= PriceFrom when both are provided.</summary>
    public decimal? PriceTo       { get; init; }

    /// <summary>
    /// When true: only products with stock > 0.
    /// When false: only products with stock = 0.
    /// When null: no stock filter applied.
    /// </summary>
    public bool?    InStock       { get; init; }

    /// <summary>
    /// Field to sort by. Allowed values: Name, Price, Newest, Oldest.
    /// Defaults to Newest when absent or invalid.
    /// </summary>
    public string   SortBy        { get; init; } = "Newest";

    /// <summary>Sort direction: asc or desc. Applies only to Name and Price sorts.</summary>
    public string   SortDirection { get; init; } = "desc";

    /// <summary>1-indexed page number. Defaults to 1.</summary>
    public int      Page          { get; init; } = 1;

    /// <summary>Items per page. Defaults to 20. Capped server-side at 100.</summary>
    public int      PageSize      { get; init; } = 20;
}
