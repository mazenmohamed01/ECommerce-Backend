namespace ECommerce.Application.Contracts;

/// <summary>Request body for PUT /api/admin/products/{productId}/images/reorder.</summary>
public sealed record ReorderProductImagesRequest
{
    /// <summary>
    /// The new display order for each image.
    /// All ImageIds must belong to the specified product — any foreign ID returns 400.
    /// </summary>
    public IReadOnlyList<ImageOrderItem> ImageOrders { get; init; } = [];
}

/// <summary>A single (ImageId, DisplayOrder) pair in a reorder request.</summary>
public sealed record ImageOrderItem
{
    public Guid ImageId      { get; init; }
    public int  DisplayOrder { get; init; }
}
