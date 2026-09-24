namespace ECommerce.Application.Contracts;

/// <summary>
/// Request DTO for customer self-registration via email and password.
/// </summary>
public sealed class RegisterRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName  { get; init; } = string.Empty;
    public string Email     { get; init; } = string.Empty;
    public string Password  { get; init; } = string.Empty;
}
