namespace ECommerce.Application.Contracts;

/// <summary>
/// Paging parameters passed from the API layer into list queries.
/// </summary>
public record PaginationRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>Max allowed page size — prevents abusive large pages.</summary>
    public const int MaxPageSize = 100;

    public int GetValidatedPageSize() =>
        Math.Min(PageSize, MaxPageSize);
}
