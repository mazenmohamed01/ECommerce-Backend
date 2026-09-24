namespace ECommerce.Application.Contracts;

public sealed record ForgotPasswordRequest
{
    public string Email { get; init; } = string.Empty;
}
