using Microsoft.AspNetCore.Http;

namespace ECommerce.Application.Contracts;

/// <summary>
/// Multipart form model for POST /api/admin/products/{id}/images.
/// ProductId is supplied via the route parameter, not this body.
/// </summary>
public sealed class UploadProductImageRequest
{
    /// <summary>The image file to upload. Required. Max 5 MB. JPEG/PNG/WebP only.</summary>
    public IFormFile File     { get; set; } = default!;

    /// <summary>Whether this image becomes the product's main (hero) image.</summary>
    public bool      IsMain   { get; set; } = false;

    /// <summary>Screen-reader accessible description. Optional, max 200 chars.</summary>
    public string?   AltText  { get; set; }
}
