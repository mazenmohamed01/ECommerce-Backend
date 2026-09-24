using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.MoyasarEventId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PaymentId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        // ── Idempotency core guarantee ─────────────────────────────────────────
        builder.HasIndex(x => x.MoyasarEventId)
            .IsUnique()
            .HasDatabaseName("uix_processedwebhookevent_moyasar_event_id");

        builder.HasIndex(x => x.PaymentId)
            .HasDatabaseName("ix_processedwebhookevent_payment_id");
    }
}
