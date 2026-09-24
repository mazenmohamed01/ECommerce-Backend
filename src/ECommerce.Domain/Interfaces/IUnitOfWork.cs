namespace ECommerce.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern: wraps a single business transaction across one or more repositories.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wraps the given operation inside a database transaction using the active execution strategy.
    /// This is the ONLY correct way to run transactions when EnableRetryOnFailure is configured.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wraps the given operation inside a database transaction and returns a result.
    /// This is the ONLY correct way to run transactions when EnableRetryOnFailure is configured.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly begins a database transaction. Use only when NOT using EnableRetryOnFailure.
    /// Prefer ExecuteInTransactionAsync for all transactional code.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits the active database transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back the active database transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
