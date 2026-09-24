using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

public sealed class OutboundWebhookEndpoint : BaseEntity
{
    public string Url { get; private set; } = default!;
    public string SecretToken { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private OutboundWebhookEndpoint() { }

    public static OutboundWebhookEndpoint Create(string url, string secretToken)
    {
        return new OutboundWebhookEndpoint
        {
            Url = url,
            SecretToken = secretToken,
            IsActive = true
        };
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }

    public void Activate()
    {
        IsActive = true;
        SetUpdatedAt();
    }
}
