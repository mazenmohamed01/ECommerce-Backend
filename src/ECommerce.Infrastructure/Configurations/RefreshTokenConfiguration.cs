using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // int identity PK — high insert volume, Guid not needed
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Token)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.RevokedAt).IsRequired(false);

        builder.Property(r => r.CreatedByIp)
            .IsRequired()
            .HasMaxLength(45);  // IPv6 max = 45 chars

        builder.Property(r => r.RevokedByIp)
            .HasMaxLength(45);

        // ── Computed properties — NOT mapped ──────────────────────────────────
        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsRevoked);
        builder.Ignore(r => r.IsActive);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(r => r.Token)
            .IsUnique()
            .HasDatabaseName("UIX_RefreshToken_Token");

        builder.HasIndex(r => r.UserId)
            .HasDatabaseName("IX_RefreshToken_UserId");

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);  // delete tokens when user is deleted

        builder.ToTable("RefreshTokens");
    }
}
