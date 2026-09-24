namespace ECommerce.Application.Contracts;

/// <summary>
/// Request DTO for Google OAuth login.
/// The frontend verifies the user with Google and sends the resulting ID token here.
/// The server validates the ID token server-side using Google.Apis.Auth.
/// </summary>
public sealed class GoogleLoginRequest
{
    /// <summary>The Google ID token (JWT) returned by the Google Sign-In client library.</summary>
    public string IdToken { get; init; } = string.Empty;
}
