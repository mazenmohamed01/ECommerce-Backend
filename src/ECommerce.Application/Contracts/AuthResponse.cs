namespace ECommerce.Application.Contracts;

/// <summary>
/// Unified authentication response returned after a successful login or registration.
/// Contains the JWT access token and the authenticated user's profile summary.
/// </summary>
public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int    ExpiresIn   { get; init; }   // seconds
    public DateTime ExpiresAtUtc { get; init; }
    public string UserId      { get; init; } = string.Empty;
    public string Email       { get; init; } = string.Empty;
    public string FullName    { get; init; } = string.Empty;
    public string Role        { get; init; } = string.Empty;
}
