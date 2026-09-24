using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Persistence;

public sealed class ProcessedWebhookEventRepository : IProcessedWebhookEventRepository
{
    private readonly ApplicationDbContext _context;

    public ProcessedWebhookEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string moyasarEventId, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessedWebhookEvents
            .AnyAsync(e => e.MoyasarEventId == moyasarEventId, cancellationToken);
    }

    public async Task AddAsync(ProcessedWebhookEvent entry, CancellationToken cancellationToken = default)
    {
        await _context.ProcessedWebhookEvents.AddAsync(entry, cancellationToken);
    }

    public async Task<(IEnumerable<ProcessedWebhookEvent> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? eventType, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProcessedWebhookEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var safePageSize = Math.Min(Math.Max(pageSize, 1), 100);
        var safePage = Math.Max(page, 1);

        var items = await query
            .OrderByDescending(e => e.ProcessedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
