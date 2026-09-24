using ECommerce.Application.Common;
using ECommerce.Application.Contracts;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ECommerce.Infrastructure.Services;

public sealed class MoyasarPaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly MoyasarSettings _settings;
    private readonly ILogger<MoyasarPaymentService> _logger;

    public MoyasarPaymentService(
        HttpClient httpClient,
        IOptions<MoyasarSettings> settings,
        ILogger<MoyasarPaymentService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<PaymentInitiationResponse>> InitiatePaymentAsync(Order order, CancellationToken cancellationToken = default)
    {
        try
        {
            // Amount in Halalas
            var amountInHalalas = (long)Math.Round(order.TotalPrice * 100, 0);

            var requestBody = new
            {
                amount = amountInHalalas,
                currency = "SAR",
                description = $"Order {order.OrderNumber}",
                // Using a fallback URL, normally injected via options if frontend URL is known
                callback_url = "https://ecommerce.com/payment/callback", 
                metadata = new { order_id = order.Id.ToString() }
            };

            _logger.LogInformation("Initiating Moyasar invoice for Order {OrderNumber}, Amount: {Amount}", order.OrderNumber, amountInHalalas);

            var response = await _httpClient.PostAsJsonAsync("/v1/invoices", requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Moyasar payment initiation failed. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return Result.Failure<PaymentInitiationResponse>(Error.BadGateway("PaymentFailed", "Failed to initiate payment with provider."));
            }

            var moyasarResponse = await response.Content.ReadFromJsonAsync<MoyasarInvoiceResponse>(cancellationToken: cancellationToken);
            
            if (moyasarResponse == null || string.IsNullOrEmpty(moyasarResponse.Id))
            {
                _logger.LogError("Moyasar returned an empty or invalid response.");
                return Result.Failure<PaymentInitiationResponse>(Error.BadGateway("InvalidResponse", "Invalid response from payment provider."));
            }

            var initiationResponse = new PaymentInitiationResponse
            {
                PaymentId = moyasarResponse.Id,
                RedirectUrl = moyasarResponse.Url ?? string.Empty,
                Amount = order.TotalPrice,
                Currency = "SAR"
            };

            return Result.Success(initiationResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while initiating payment for Order {OrderNumber}", order.OrderNumber);
            return Result.Failure<PaymentInitiationResponse>(Error.Failure("PaymentException", "An unexpected error occurred during payment initiation."));
        }
    }

    private class MoyasarInvoiceResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }
}
