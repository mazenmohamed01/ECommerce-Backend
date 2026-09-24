using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        // ── Identity ──────────────────────────────────────────────────────────
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(20);

        // ── Customer Snapshot ─────────────────────────────────────────────────
        builder.Property(o => o.CustomerId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.CustomerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CustomerEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(o => o.CustomerPhone)
            .IsRequired()
            .HasMaxLength(20);

        // ── Shipping Address (Owned Entity) ───────────────────────────────────
        builder.OwnsOne(o => o.ShippingAddress, addr =>
        {
            addr.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("ShippingStreet");

            addr.Property(a => a.District)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("ShippingDistrict");

            addr.Property(a => a.City)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnName("ShippingCity");

            addr.Property(a => a.State)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("ShippingState");

            addr.Property(a => a.PostalCode)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("ShippingPostalCode");

            addr.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(60)
                .HasColumnName("ShippingCountry")
                .HasDefaultValue("Saudi Arabia");
        });

        // ── Notes ─────────────────────────────────────────────────────────────
        builder.Property(o => o.Notes)
            .HasMaxLength(500);

        // ── Pricing — numeric(18,2) is the correct PostgreSQL type ────────────
        builder.Property(o => o.SubTotal)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(o => o.ShippingCost)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.DiscountAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m);

        builder.Property(o => o.TotalPrice)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        // ── Payment — stored as strings ───────────────────────────────────────
        builder.Property(o => o.PaymentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.PaymentMethod)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.PaymentGateway)
            .HasMaxLength(30);

        builder.Property(o => o.PaymentId)
            .HasMaxLength(100);

        builder.Property(o => o.TransactionId)
            .HasMaxLength(100);

        builder.Property(o => o.PaidAt).IsRequired(false);

        // ── Order Status ──────────────────────────────────────────────────────
        builder.Property(o => o.OrderStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // ── PostgreSQL optimistic concurrency via xmin ────────────────────────
        builder.Property<uint>("Version")
            .IsRowVersion();

        // ── Audit ─────────────────────────────────────────────────────────────
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired(false);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("uix_order_order_number");

        builder.HasIndex(o => o.CustomerEmail)
            .HasDatabaseName("ix_order_customer_email");

        builder.HasIndex(o => o.OrderStatus)
            .HasDatabaseName("ix_order_order_status");

        builder.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("ix_order_created_at");

        builder.HasIndex(o => o.CustomerId)
            .HasDatabaseName("ix_order_customer_id");

        builder.HasIndex(o => new { o.OrderStatus, o.PaymentMethod, o.CreatedAt })
            .HasDatabaseName("ix_order_status_payment_created_at");

        builder.HasIndex(o => o.PaymentId)
            .HasDatabaseName("ix_order_payment_id");

        // ── Relationships ─────────────────────────────────────────────────────
        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Check Constraints (PostgreSQL syntax: "ColumnName") ───────────────
        builder.ToTable("Orders", t =>
        {
            t.HasCheckConstraint("ck_order_sub_total_non_negative",
                "\"SubTotal\" >= 0");
            t.HasCheckConstraint("ck_order_shipping_cost_non_negative",
                "\"ShippingCost\" >= 0");
            t.HasCheckConstraint("ck_order_discount_amount_non_negative",
                "\"DiscountAmount\" >= 0");
        });
    }
}
