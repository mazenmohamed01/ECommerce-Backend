using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

/// <summary>
/// Customer-facing authentication service.
/// Handles registration, email/password login, Google OAuth login, and profile retrieval.
/// </summary>
public interface IAuthService
{
    /// <summary>Registers a new customer using email and password.</summary>
    Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Authenticates a customer using email and password.</summary>
    Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a customer via Google OAuth.
    /// Creates a new account automatically if the email does not exist.
    /// Links the Google account if the email already exists.
    /// </summary>
    Task<Result<AuthResponse>> GoogleLoginAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the profile of the currently authenticated user.</summary>
    Task<Result<UserProfileResponse>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<Result> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refreshes the access token using a valid refresh token.</summary>
    Task<Result<AuthResponse>> RefreshTokenAsync(
        string refreshToken,
        string ipAddress,
        CancellationToken cancellationToken = default);
}
