using System.Text.Json.Serialization;

namespace ECommerce.Application.Contracts;

public sealed record WebhookAckResponse
{
    [JsonPropertyName("received")]
    public bool Received { get; init; } = true;
}
