namespace ECommerce.Infrastructure.Services;

/// <summary>
/// Configuration options for Cloudinary integration.
/// Bound from appsettings.json under the "Cloudinary" section.
/// Secrets (ApiKey, ApiSecret) must be stored in User Secrets (dev) or Azure Key Vault (prod).
/// </summary>
public sealed class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    /// <summary>Cloudinary cloud name (e.g. "my-shop").</summary>
    public string CloudName    { get; init; } = default!;

    /// <summary>Cloudinary API key. Never log or return this in responses.</summary>
    public string ApiKey       { get; init; } = default!;

    /// <summary>Cloudinary API secret. Never log or return this in responses.</summary>
    public string ApiSecret    { get; init; } = default!;

    /// <summary>Default upload folder prefix (e.g. "ecommerce/products").</summary>
    public string UploadFolder { get; init; } = "ecommerce";
}
