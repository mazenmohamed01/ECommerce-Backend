namespace ECommerce.Domain.Common;

/// <summary>
/// Base entity — provides Id, CreatedAt, UpdatedAt for all auditable domain entities.
/// ProductImage intentionally does NOT inherit this.
/// </summary>
public abstract class BaseEntity
{
    public Guid      Id        { get; protected set; } = Guid.NewGuid();
    public DateTime  CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }

    protected void SetUpdatedAt() => UpdatedAt = DateTime.UtcNow;
}
