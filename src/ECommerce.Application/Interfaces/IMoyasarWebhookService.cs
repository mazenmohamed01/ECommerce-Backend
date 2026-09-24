using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Interfaces;

public interface IMoyasarWebhookService
{
    Task<Result<WebhookAckResponse>> ProcessWebhookAsync(MoyasarWebhookPayload payload, string configuredSecret, CancellationToken cancellationToken = default);
}
