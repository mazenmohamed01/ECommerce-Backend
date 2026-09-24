using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);

        // ── Snapshot Properties ───────────────────────────────────────────────
        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.ProductSku)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.UnitPrice)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.TotalPrice)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        // ── Audit ─────────────────────────────────────────────────────────────
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired(false);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(i => i.OrderId)
            .HasDatabaseName("ix_order_item_order_id");

        builder.HasIndex(i => i.ProductId)
            .HasDatabaseName("ix_order_item_product_id");

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Check Constraints (PostgreSQL syntax) ─────────────────────────────
        builder.ToTable("OrderItems", t =>
        {
            t.HasCheckConstraint("ck_order_item_quantity_positive",
                "\"Quantity\" > 0");
            t.HasCheckConstraint("ck_order_item_unit_price_non_negative",
                "\"UnitPrice\" >= 0");
        });
    }
}
