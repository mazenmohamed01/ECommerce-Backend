namespace ECommerce.Application.Contracts;

/// <summary>
/// Response DTO for GET /api/auth/me — returns current authenticated user's profile.
/// </summary>
public sealed class UserProfileResponse
{
    public string  UserId      { get; init; } = string.Empty;
    public string  Email       { get; init; } = string.Empty;
    public string  FirstName   { get; init; } = string.Empty;
    public string  LastName    { get; init; } = string.Empty;
    public string  FullName    { get; init; } = string.Empty;
    public string  Role        { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt  { get; init; }
}
