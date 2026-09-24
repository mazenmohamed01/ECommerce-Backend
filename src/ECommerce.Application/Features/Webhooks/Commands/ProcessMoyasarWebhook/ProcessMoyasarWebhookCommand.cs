using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;

namespace ECommerce.Application.Features.Webhooks.Commands.ProcessMoyasarWebhook;

public sealed record ProcessMoyasarWebhookCommand(MoyasarWebhookPayload Payload, string ConfiguredSecret) : ICommand<Result<WebhookAckResponse>>;
