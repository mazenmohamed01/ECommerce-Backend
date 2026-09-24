namespace ECommerce.Application.Contracts;

/// <summary>
/// Shared login DTO used for both Customer and Admin email/password authentication.
/// </summary>
public sealed class LoginRequest
{
    public string Email    { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
