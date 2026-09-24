using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class OutboundWebhookEventConfiguration : IEntityTypeConfiguration<OutboundWebhookEvent>
{
    public void Configure(EntityTypeBuilder<OutboundWebhookEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Payload).HasColumnType("jsonb");
        
        builder.HasOne(e => e.Endpoint)
               .WithMany()
               .HasForeignKey(e => e.EndpointId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.Status, e.NextRetryAt });
    }
}
