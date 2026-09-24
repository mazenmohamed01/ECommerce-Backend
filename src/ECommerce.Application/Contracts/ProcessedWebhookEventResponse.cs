namespace ECommerce.Application.Contracts;

public sealed record ProcessedWebhookEventResponse
{
    public Guid Id { get; init; }
    public string MoyasarEventId { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; }
}
