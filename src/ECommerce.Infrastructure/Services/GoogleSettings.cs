namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Strongly-typed settings for Google OAuth.
/// Bound from appsettings.json → "Google" section.
/// The ClientId is safe to commit; ClientSecret must be stored in User Secrets or environment variables.
/// </summary>
public sealed class GoogleSettings
{
    public const string SectionName = "Google";

    /// <summary>OAuth 2.0 Client ID from the Google Cloud Console.</summary>
    public string ClientId { get; init; } = string.Empty;
}
