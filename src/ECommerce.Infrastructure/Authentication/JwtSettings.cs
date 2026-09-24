namespace ECommerce.Infrastructure.Authentication;

/// <summary>
/// Strongly-typed settings for JWT token generation.
/// Bound from appsettings.json → "Jwt" section via IOptions&lt;JwtSettings&gt;.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    /// <summary>Secret key used to sign tokens. Min 32 characters recommended.</summary>
    public string Secret { get; init; } = string.Empty;

    /// <summary>Token lifetime in minutes.</summary>
    public int ExpiryMinutes { get; init; } = 60;
}
