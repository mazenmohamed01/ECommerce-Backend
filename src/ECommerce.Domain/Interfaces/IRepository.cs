namespace ECommerce.Domain.Interfaces;

/// <summary>
/// Generic repository abstraction — data access contract owned by the Domain layer.
/// Concrete implementations live in Infrastructure.
/// </summary>
/// <typeparam name="TEntity">The aggregate root or entity type.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
