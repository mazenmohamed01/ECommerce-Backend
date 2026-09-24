namespace ECommerce.Application.Contracts;

public sealed record PaymentInitiationResponse
{
    public string PaymentId { get; init; } = string.Empty;
    public string RedirectUrl { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "SAR";
}
