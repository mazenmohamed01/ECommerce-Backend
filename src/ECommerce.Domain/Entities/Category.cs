using ECommerce.Domain.Common;

namespace ECommerce.Domain.Entities;

/// <summary>
/// Category aggregate root.
/// Restricts delete: a category with products cannot be deleted — archive it with IsActive = false.
/// </summary>
public sealed class Category : AggregateRoot
{
    public string  Name              { get; private set; } = default!;
    public string  Slug              { get; private set; } = default!;
    public string? Description       { get; private set; }
    public string? ImageUrl          { get; private set; }
    /// <summary>Cloudinary PublicId of the current category image. Null when no image is stored.</summary>
    public string? ImagePublicId     { get; private set; }
    public int     SortOrder         { get; private set; }
    public bool    IsActive          { get; private set; } = true;

    private readonly List<Product> _products = [];
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Category() { }  // EF Core

    public static Category Create(
        string name,
        string slug,
        string? description = null,
        string? imageUrl    = null,
        int sortOrder       = 0)
    {
        return new Category
        {
            Name        = name,
            Slug        = slug,
            Description = description,
            ImageUrl    = imageUrl,
            SortOrder   = sortOrder
        };
    }

    /// <summary>Updates the category hero image after a successful Cloudinary upload.</summary>
    public void UpdateImage(string imageUrl, string imagePublicId)
    {
        ImageUrl      = imageUrl;
        ImagePublicId = imagePublicId;
        SetUpdatedAt();
    }

    /// <summary>Clears the category hero image (call after Cloudinary deletion).</summary>
    public void RemoveImage()
    {
        ImageUrl      = null;
        ImagePublicId = null;
        SetUpdatedAt();
    }

    public void Update(
        string name,
        string slug,
        string? description = null,
        string? imageUrl    = null,
        int sortOrder       = 0,
        bool isActive       = true)
    {
        Name        = name;
        Slug        = slug;
        Description = description;
        ImageUrl    = imageUrl;
        SortOrder   = sortOrder;
        IsActive    = isActive;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
