namespace ECommerce.Domain.Abstractions;

/// <summary>
/// Contract for aggregate roots that raise domain events.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
