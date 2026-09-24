namespace ECommerce.Infrastructure.Identity;

/// <summary>
/// JWT refresh token — stored in the database, one per device/session.
/// Cleaner than extending AspNetUsers with token columns.
/// int PK: high insert volume, no need for Guid here.
/// </summary>
public sealed class RefreshToken
{
    public int       Id          { get; private set; }
    public string    UserId      { get; private set; } = default!;  // IdentityUser.Id (string)
    public string    Token       { get; private set; } = default!;  // cryptographically random
    public DateTime  ExpiresAt   { get; private set; }
    public DateTime? RevokedAt   { get; private set; }
    public DateTime  CreatedAt   { get; private set; } = DateTime.UtcNow;
    public string    CreatedByIp { get; private set; } = default!;
    public string?   RevokedByIp { get; private set; }

    // Computed — NOT mapped to DB columns
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked  => RevokedAt.HasValue;
    public bool IsActive   => !IsExpired && !IsRevoked;

    public ApplicationUser User { get; private set; } = default!;

    private RefreshToken() { }  // EF Core

    public static RefreshToken Create(
        string   userId,
        string   token,
        DateTime expiresAt,
        string   createdByIp)
    {
        return new RefreshToken
        {
            UserId      = userId,
            Token       = token,
            ExpiresAt   = expiresAt,
            CreatedByIp = createdByIp
        };
    }

    public void Revoke(string revokedByIp)
    {
        RevokedAt   = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
    }
}
