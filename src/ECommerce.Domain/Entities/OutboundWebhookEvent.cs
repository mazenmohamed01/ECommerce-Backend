using ECommerce.Domain.Common;
using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Entities;

public sealed class OutboundWebhookEvent : BaseEntity
{
    public Guid EndpointId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public OutboundWebhookStatus Status { get; private set; } = OutboundWebhookStatus.Pending;
    public int AttemptCount { get; private set; } = 0;
    public DateTime? NextRetryAt { get; private set; }
    public string? LastError { get; private set; }

    public OutboundWebhookEndpoint Endpoint { get; private set; } = default!;

    private OutboundWebhookEvent() { }

    public static OutboundWebhookEvent Create(Guid endpointId, string eventType, string payload)
    {
        return new OutboundWebhookEvent
        {
            EndpointId = endpointId,
            EventType = eventType,
            Payload = payload,
            Status = OutboundWebhookStatus.Pending,
            AttemptCount = 0,
            NextRetryAt = DateTime.UtcNow
        };
    }

    public void MarkAsSuccess()
    {
        Status = OutboundWebhookStatus.Success;
        NextRetryAt = null;
        SetUpdatedAt();
    }

    public void MarkAsFailed(string error, DateTime? nextRetryAt = null)
    {
        Status = OutboundWebhookStatus.Failed;
        LastError = error;
        NextRetryAt = nextRetryAt;
        SetUpdatedAt();
    }
    
    public void IncrementAttempt()
    {
        AttemptCount++;
        SetUpdatedAt();
    }
}
