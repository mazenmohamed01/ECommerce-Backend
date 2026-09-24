namespace ECommerce.Infrastructure.Services;

public sealed class N8nSettings
{
    public const string SectionName = "N8nSettings";

    public string OrderNotificationWebhookUrl { get; set; } = string.Empty;
}
