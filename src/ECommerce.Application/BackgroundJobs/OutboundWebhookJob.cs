using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using ECommerce.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.BackgroundJobs;

public sealed class OutboundWebhookJob
{
    private readonly IOutboundWebhookEventRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OutboundWebhookJob> _logger;

    public OutboundWebhookJob(
        IOutboundWebhookEventRepository repository,
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        ILogger<OutboundWebhookJob> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("OutboundWebhookJob started.");

        var events = await _repository.GetPendingEventsAsync(50, cancellationToken);

        if (!events.Any())
        {
            _logger.LogInformation("No pending outbound webhooks found.");
            return;
        }

        _logger.LogInformation("Found {Count} pending outbound webhooks to process.", events.Count);

        var client = _httpClientFactory.CreateClient("OutboundWebhookClient");

        foreach (var evt in events)
        {
            try
            {
                var content = new StringContent(evt.Payload, Encoding.UTF8, "application/json");
                
                var signature = GenerateSignature(evt.Payload, evt.Endpoint.SecretToken);
                content.Headers.Add("X-Signature", signature);

                var response = await client.PostAsync(evt.Endpoint.Url, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    evt.MarkAsSuccess();
                    _logger.LogInformation("Successfully sent webhook {EventId} to {Url}", evt.Id, evt.Endpoint.Url);
                }
                else
                {
                    var errorDetails = $"Status Code: {response.StatusCode}";
                    HandleFailure(evt, errorDetails);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send webhook {EventId} to {Url}", evt.Id, evt.Endpoint.Url);
                HandleFailure(evt, ex.Message);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("OutboundWebhookJob completed successfully.");
    }

    private void HandleFailure(ECommerce.Domain.Entities.OutboundWebhookEvent evt, string error)
    {
        evt.IncrementAttempt();

        if (evt.AttemptCount >= 5)
        {
            evt.MarkAsFailed($"Permanent Failure after 5 attempts. Last Error: {error}");
            _logger.LogError("Webhook {EventId} failed permanently.", evt.Id);
        }
        else
        {
            var nextRetry = DateTime.UtcNow.AddMinutes(Math.Pow(2, evt.AttemptCount));
            evt.MarkAsFailed(error, nextRetry);
            _logger.LogWarning("Webhook {EventId} failed. Retry {Attempt} at {NextRetry}", evt.Id, evt.AttemptCount, nextRetry);
        }
    }

    private string GenerateSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
