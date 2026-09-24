using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class ProcessedWebhookEvent : BaseEntity
{
    public string MoyasarEventId { get; private set; } = default!;
    public string PaymentId { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public DateTime ProcessedAt { get; private set; }

    private ProcessedWebhookEvent() { } // EF Core

    public static ProcessedWebhookEvent Create(string moyasarEventId, string paymentId, string eventType)
    {
        return new ProcessedWebhookEvent
        {
            MoyasarEventId = moyasarEventId,
            PaymentId = paymentId,
            EventType = eventType,
            ProcessedAt = DateTime.UtcNow
        };
    }
}
