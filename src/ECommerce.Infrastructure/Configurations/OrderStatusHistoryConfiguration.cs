using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistory");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();

        // ── Properties ────────────────────────────────────────────────────────
        builder.Property(h => h.PreviousStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.NewStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(h => h.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(h => h.Reason)
            .HasMaxLength(500);

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        // ── Audit ─────────────────────────────────────────────────────────────
        builder.Property(h => h.CreatedAt).IsRequired();
        builder.Property(h => h.UpdatedAt).IsRequired(false);

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(h => h.OrderId)
            .HasDatabaseName("ix_order_status_history_order_id");

        // ── Relationships ─────────────────────────────────────────────────────
        // Handled from OrderConfiguration (HasMany) but we can reinforce it here
        builder.HasOne<Order>()
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
