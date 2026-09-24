using System.Text;
using System.Text.RegularExpressions;
using ECommerce.Application.Interfaces;

namespace ECommerce.Application.Services;

/// <summary>
/// Pure stateless slug utility — no I/O, no external dependencies.
/// Registered as Singleton so all services share the same instance without allocation overhead.
/// </summary>
public sealed class SlugService : ISlugService
{
    private static readonly Regex NonAlphanumericOrHyphen = new(
        @"[^\p{L}\p{N}-]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly Regex MultipleHyphens = new(
        @"-{2,}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    /// <inheritdoc/>
    public string GenerateSlug(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // 1. Lowercase and normalize Unicode (decompose accented chars)
        var normalized = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        // 2. Build ASCII representation — drop combining characters, keep base letters
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var ascii = sb.ToString();

        // 3. Replace spaces and underscores with hyphens
        ascii = ascii.Replace(' ', '-').Replace('_', '-');

        // 4. Strip all characters that are not lowercase-alphanumeric or hyphen
        ascii = NonAlphanumericOrHyphen.Replace(ascii, string.Empty);

        // 5. Collapse consecutive hyphens and trim edge hyphens
        ascii = MultipleHyphens.Replace(ascii, "-").Trim('-');

        return string.IsNullOrEmpty(ascii) ? "product" : ascii;
    }

    /// <inheritdoc/>
    public async Task<string> EnsureUniqueSlugAsync(
        string baseSlug,
        Func<string, Task<bool>> existsAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseSlug);

        // Fast path — slug is already unique
        if (!await existsAsync(baseSlug))
            return baseSlug;

        // Append numeric suffix until a free slot is found
        for (var counter = 2; counter <= 1000; counter++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = $"{baseSlug}-{counter}";
            if (!await existsAsync(candidate))
                return candidate;
        }

        // Extremely unlikely — add timestamp fallback to avoid infinite growth
        return $"{baseSlug}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    }
}
