namespace ECommerce.Domain.Interfaces;

/// <summary>
/// Domain service interface for generating unique, sequential order numbers.
/// </summary>
public interface IOrderNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
