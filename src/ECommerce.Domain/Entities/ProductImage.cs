namespace ECommerce.Domain.Entities;

/// <summary>
/// Product image — child of the Product aggregate.
/// Does NOT inherit BaseEntity: images have no lifecycle audit requirement.
/// Id is a Guid assigned by the entity (ValueGeneratedNever in EF config).
/// </summary>
public sealed class ProductImage
{
    public Guid    Id                { get; private set; } = Guid.NewGuid();
    public Guid    ProductId         { get; private set; }
    public string  ImageUrl          { get; private set; } = default!;
    public string  CloudinaryPublicId { get; private set; } = default!;
    public string? AltText           { get; private set; }
    public int     DisplayOrder      { get; private set; }
    public bool    IsMain            { get; private set; }

    public Product Product { get; private set; } = default!;

    private ProductImage() { }  // EF Core

    public static ProductImage Create(
        Guid    productId,
        string  imageUrl,
        string  cloudinaryPublicId,
        int     displayOrder,
        bool    isMain   = false,
        string? altText  = null)
    {
        return new ProductImage
        {
            ProductId          = productId,
            ImageUrl           = imageUrl,
            CloudinaryPublicId = cloudinaryPublicId,
            DisplayOrder       = displayOrder,
            IsMain             = isMain,
            AltText            = altText
        };
    }

    public void SetAsMain()            => IsMain = true;
    public void UnsetAsMain()          => IsMain = false;
    public void SetDisplayOrder(int o) => DisplayOrder = o;
}
