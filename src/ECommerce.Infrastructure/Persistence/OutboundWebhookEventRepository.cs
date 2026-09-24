using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class OutboundWebhookEventRepository : Repository<OutboundWebhookEvent>, IOutboundWebhookEventRepository
{
    public OutboundWebhookEventRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<OutboundWebhookEvent>> GetPendingEventsAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await DbSet
            .Include(e => e.Endpoint)
            .Where(e => (e.Status == OutboundWebhookStatus.Pending || e.Status == OutboundWebhookStatus.Failed) 
                     && e.NextRetryAt <= now
                     && e.Endpoint.IsActive)
            .OrderBy(e => e.NextRetryAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
