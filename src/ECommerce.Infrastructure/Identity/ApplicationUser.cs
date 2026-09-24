using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

/// <summary>
/// Extends IdentityUser with domain profile fields.
/// Keep extension fields minimal — Identity handles authentication concerns.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    public string?   FirstName     { get; set; }
    public string?   LastName      { get; set; }
    public DateTime  CreatedAt     { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    /// <summary>Convenience: full display name for admin UI.</summary>
    public string DisplayName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? UserName ?? Email ?? Id
            : $"{FirstName} {LastName}".Trim();
}
