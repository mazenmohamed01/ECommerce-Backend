using ECommerce.Application.Abstractions;
using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Features.Webhooks.Commands.ProcessMoyasarWebhook;

internal sealed class ProcessMoyasarWebhookCommandHandler : ICommandHandler<ProcessMoyasarWebhookCommand, Result<WebhookAckResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderRepository _orderRepository;
    private readonly IProcessedWebhookEventRepository _webhookEventRepository;
    private readonly IStockService _stockService;
    private readonly ILogger<ProcessMoyasarWebhookCommandHandler> _logger;

    public ProcessMoyasarWebhookCommandHandler(
        IUnitOfWork unitOfWork,
        IOrderRepository orderRepository,
        IProcessedWebhookEventRepository webhookEventRepository,
        IStockService stockService,
        ILogger<ProcessMoyasarWebhookCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
        _webhookEventRepository = webhookEventRepository;
        _stockService = stockService;
        _logger = logger;
    }

    public async Task<Result<WebhookAckResponse>> Handle(ProcessMoyasarWebhookCommand command, CancellationToken cancellationToken)
    {
        var payload = command.Payload;
        var configuredSecret = command.ConfiguredSecret;

        if (string.IsNullOrEmpty(configuredSecret) || payload.SecretToken != configuredSecret)
        {
            _logger.LogWarning("Webhook unauthorized: secret token mismatch. EventId: {EventId}", payload.Id);
            return Result.Failure<WebhookAckResponse>(Error.Unauthorized("WebhookUnauthorized", "Invalid secret token."));
        }

        var alreadyProcessed = await _webhookEventRepository.ExistsAsync(payload.Id, cancellationToken);
        if (alreadyProcessed)
        {
            _logger.LogInformation("Webhook event {EventId} was already processed. Returning ACK.", payload.Id);
            return Result.Success(new WebhookAckResponse { Received = true });
        }

        try
        {
            return await _unitOfWork.ExecuteInTransactionAsync<Result<WebhookAckResponse>>(async ct =>
            {
                var paymentId = payload.Data.Id;
                var order = await _orderRepository.GetByPaymentIdAsync(paymentId, ct);
                if (order == null)
                {
                    _logger.LogWarning("Webhook received for unknown PaymentId {PaymentId}. EventId: {EventId}", paymentId, payload.Id);
                    return Result.Success(new WebhookAckResponse { Received = true });
                }

                if (payload.Type == "payment.paid" && payload.Data.Status == "paid")
                {
                    if (order.PaymentStatus != PaymentStatus.Paid)
                    {
                        var oldStatus = order.OrderStatus;
                        order.MarkAsPaid("Moyasar", paymentId, payload.Id);

                        var history = OrderStatusHistory.Create(order.Id, oldStatus, order.OrderStatus, "System", "Payment successful via webhook");
                        await _orderRepository.AddStatusHistoryAsync(history, ct);

                        foreach (var item in order.Items)
                        {
                            var deduceResult = await _stockService.ConfirmStockAsync(item.ProductId, item.Quantity, ct);
                            if (deduceResult.IsFailure)
                            {
                                _logger.LogError("Failed to deduce stock for order {OrderId}. Error: {Error}", order.Id, deduceResult.Error.Message);
                                return Result.Failure<WebhookAckResponse>(deduceResult.Error);
                            }
                        }
                    }
                }
                else if (payload.Type == "payment.failed" && payload.Data.Status == "failed")
                {
                    if (order.PaymentStatus != PaymentStatus.Failed && order.PaymentStatus != PaymentStatus.Paid)
                    {
                        var oldStatus = order.OrderStatus;
                        order.MarkPaymentFailed();

                        var history = OrderStatusHistory.Create(order.Id, oldStatus, order.OrderStatus, "System", "Payment failed via webhook");
                        await _orderRepository.AddStatusHistoryAsync(history, ct);

                        foreach (var item in order.Items)
                        {
                            var releaseResult = await _stockService.ReleaseStockAsync(item.ProductId, item.Quantity, ct);
                            if (releaseResult.IsFailure)
                            {
                                _logger.LogError("Failed to release reserved stock for order {OrderId}. Error: {Error}", order.Id, releaseResult.Error.Message);
                                return Result.Failure<WebhookAckResponse>(releaseResult.Error);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("Unhandled webhook event type {EventType} for EventId: {EventId}", payload.Type, payload.Id);
                }

                var processedEvent = ProcessedWebhookEvent.Create(payload.Id, paymentId, payload.Type);
                await _webhookEventRepository.AddAsync(processedEvent, ct);

                await _unitOfWork.SaveChangesAsync(ct);

                _logger.LogInformation("Webhook event {EventId} processed successfully for Order {OrderNumber}", payload.Id, order.OrderNumber);
                return Result.Success(new WebhookAckResponse { Received = true });
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventId}", payload.Id);
            return Result.Failure<WebhookAckResponse>(Error.Failure("WebhookProcessingFailed", "An error occurred while processing the webhook."));
        }
    }
}
