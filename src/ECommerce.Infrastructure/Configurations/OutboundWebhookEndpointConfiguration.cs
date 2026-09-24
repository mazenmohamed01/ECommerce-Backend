using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Configurations;

public sealed class OutboundWebhookEndpointConfiguration : IEntityTypeConfiguration<OutboundWebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<OutboundWebhookEndpoint> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Url).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.SecretToken).IsRequired().HasMaxLength(255);
    }
}
