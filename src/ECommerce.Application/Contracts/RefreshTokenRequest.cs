namespace ECommerce.Application.Contracts;

/// <summary>
/// Payload for refreshing an authentication token.
/// </summary>
public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
