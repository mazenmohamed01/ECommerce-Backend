namespace ECommerce.Application.Interfaces;

/// <summary>
/// Provides JWT tokens for authenticated users.
/// Abstracted in Application to avoid taking a dependency on infrastructure JWT libraries.
/// </summary>
public interface IJwtProvider
{
    string GenerateToken(
        string userId,
        string email,
        string firstName,
        string lastName,
        IList<string> roles);
}
