using System.Net.Http.Json;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ECommerce.Infrastructure.Services;

public sealed class N8nIntegrationService : IN8nIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly N8nSettings _settings;
    private readonly ILogger<N8nIntegrationService> _logger;

    public N8nIntegrationService(HttpClient httpClient, IOptions<N8nSettings> options, ILogger<N8nIntegrationService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendOrderCreatedEventAsync(OrderResponse order, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.OrderNotificationWebhookUrl))
        {
            _logger.LogWarning("N8nSettings:OrderNotificationWebhookUrl is not configured. Webhook will not be sent.");
            return;
        }

        try
        {
            var payload = new
            {
                order = order,
                eventType = "OrderCreated",
                timestamp = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(_settings.OrderNotificationWebhookUrl, payload, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent OrderCreated event for Order {OrderNumber} to n8n.", order.OrderNumber);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send OrderCreated event to n8n. Status: {StatusCode}, Content: {Content}", response.StatusCode, content);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while sending OrderCreated event to n8n for Order {OrderNumber}.", order.OrderNumber);
        }
    }
}
