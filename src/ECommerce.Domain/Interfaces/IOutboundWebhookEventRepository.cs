using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IOutboundWebhookEventRepository : IRepository<OutboundWebhookEvent>
{
    Task<List<OutboundWebhookEvent>> GetPendingEventsAsync(int batchSize, CancellationToken cancellationToken = default);
}
