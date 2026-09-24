using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(i => i.Id);
        
        // Critical fix: Tell EF Core the Guid is generated client-side (via BaseEntity)
        // so it doesn't treat non-default Guids as 'Modified' when attached via navigation collections.
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Quantity)
            .IsRequired();

        // Business rule: Prevents duplicate product rows in the same cart
        builder.HasIndex(i => new { i.CartId, i.ProductId })
            .IsUnique();

        // Product FK -> RESTRICT delete
        // Never allow a product hard-delete to silently orphan cart items.
        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
