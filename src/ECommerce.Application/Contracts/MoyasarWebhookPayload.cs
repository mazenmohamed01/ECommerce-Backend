namespace ECommerce.Application.Contracts;

public sealed record MoyasarWebhookPayload
{
    public string Id { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string SecretToken { get; init; } = string.Empty;
    public MoyasarWebhookData Data { get; init; } = new();
}

public sealed record MoyasarWebhookData
{
    public string Id { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? InvoiceId { get; init; }
}
