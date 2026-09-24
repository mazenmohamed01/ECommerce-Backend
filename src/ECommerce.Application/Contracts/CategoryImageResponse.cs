namespace ECommerce.Application.Contracts;

/// <summary>
/// Response DTO returned after a successful category image upload.
/// </summary>
public sealed class CategoryImageResponse
{
    public string ImageUrl { get; init; } = string.Empty;
    public string? AltText { get; init; }
}
