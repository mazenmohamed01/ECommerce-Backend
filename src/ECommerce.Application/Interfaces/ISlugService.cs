namespace ECommerce.Application.Interfaces;

/// <summary>
/// Pure slug generation and uniqueness-enforcement utility.
/// Registered as Singleton — has no state or I/O, only string transformations.
/// </summary>
public interface ISlugService
{
    /// <summary>
    /// Generates a URL-friendly slug from a display name.
    /// Lowercases, replaces spaces with hyphens, strips non-alphanumeric characters.
    /// </summary>
    string GenerateSlug(string name);

    /// <summary>
    /// Ensures the base slug is unique by appending -2, -3, ... until a free slot is found.
    /// The <paramref name="existsAsync"/> delegate performs the uniqueness check against the DB.
    /// </summary>
    Task<string> EnsureUniqueSlugAsync(
        string baseSlug,
        Func<string, Task<bool>> existsAsync,
        CancellationToken cancellationToken = default);
}
