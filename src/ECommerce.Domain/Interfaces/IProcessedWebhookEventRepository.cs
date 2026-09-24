using ECommerce.Domain.Entities;

namespace ECommerce.Domain.Interfaces;

public interface IProcessedWebhookEventRepository
{
    Task<bool> ExistsAsync(string moyasarEventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedWebhookEvent entry, CancellationToken cancellationToken = default);
    
    Task<(IEnumerable<ProcessedWebhookEvent> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? eventType, 
        CancellationToken cancellationToken = default);
}
