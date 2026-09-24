using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(50);



        // PostgreSQL numeric type — use "numeric" not "decimal"
        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(p => p.QuantityInStock)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.ReservedStock)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.DeletedAt)
            .IsRequired(false);

        // PostgreSQL optimistic concurrency via xmin system column
        builder.Property<uint>("Version")
            .IsRowVersion();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired(false);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(p => p.Slug)
            .IsUnique()
            .HasDatabaseName("uix_product_slug");

        builder.HasIndex(p => p.Sku)
            .IsUnique()
            .HasDatabaseName("uix_product_sku");

        builder.HasIndex(p => p.CategoryId)
            .HasDatabaseName("ix_product_category_id");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("ix_product_is_active");

        builder.HasIndex(p => p.Price)
            .HasDatabaseName("ix_product_price");

        builder.HasIndex(p => new { p.IsActive, p.CategoryId })
            .HasDatabaseName("ix_product_is_active_category_id");

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Check Constraints (PostgreSQL syntax: double-quoted column names) ──
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("ck_product_price_non_negative",
                "\"Price\" >= 0");
            t.HasCheckConstraint("ck_product_stock_non_negative",
                "\"QuantityInStock\" >= 0");
            t.HasCheckConstraint("ck_product_reserved_stock_valid",
                "\"ReservedStock\" >= 0 AND \"ReservedStock\" <= \"QuantityInStock\"");
        });
    }
}
