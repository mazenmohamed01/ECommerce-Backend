using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Persistence;

/// <summary>
/// EF Core Unit of Work implementation.
/// Delegates to ApplicationDbContext.SaveChangesAsync so all repositories
/// sharing the same DbContext participate in a single transaction.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// Executes the given operation inside a PostgreSQL transaction, wrapped in the active
    /// execution strategy so that EnableRetryOnFailure is honoured without conflict.
    /// </summary>
    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync<ApplicationDbContext, int>(
            _context,
            async (dbContext, state, ct) =>
            {
                await using var tx = await state.Database.BeginTransactionAsync(ct);
                try
                {
                    await operation(ct);
                    await tx.CommitAsync(ct);
                    return 0; // Dummy result required by the interface
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken);
    }

    /// <summary>
    /// Executes the given operation inside a PostgreSQL transaction and returns a result.
    /// Wrapped in the active execution strategy so EnableRetryOnFailure is honoured.
    /// </summary>
    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync<ApplicationDbContext, TResult>(
            _context,
            async (dbContext, state, ct) =>
            {
                await using var tx = await state.Database.BeginTransactionAsync(ct);
                try
                {
                    var result = await operation(ct);
                    await tx.CommitAsync(ct);
                    return result;
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            },
            verifySucceeded: null,
            cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction == null)
        {
            await _context.Database.BeginTransactionAsync(cancellationToken);
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.CommitAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CurrentTransaction.RollbackAsync(cancellationToken);
        }
    }
}
