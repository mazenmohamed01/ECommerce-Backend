using System.ComponentModel.DataAnnotations;

namespace ECommerce.Infrastructure.Services;

public sealed class MoyasarSettings
{
    public const string SectionName = "Moyasar";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string WebhookSecret { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = "https://api.moyasar.com";
}
