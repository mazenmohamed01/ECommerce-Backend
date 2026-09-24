using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerId)
            .IsRequired()
            .HasMaxLength(450); // Matches AspNetUsers.Id default length

        // Business rule: Exactly one Cart per Customer
        builder.HasIndex(c => c.CustomerId)
            .IsUnique();

        // Foreign key to AspNetUsers.Id with Cascade delete
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Navigation to CartItems (backing field configuration)
        var navigation = builder.Metadata.FindNavigation(nameof(Cart.Items));
        navigation?.SetPropertyAccessMode(PropertyAccessMode.Field);

        // Relationship mapping
        builder.HasMany(c => c.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
