using ECommerce.Domain.Abstractions;

namespace ECommerce.Domain.Common;

/// <summary>
/// Base for aggregate roots that raise domain events.
/// Extends BaseEntity directly — no audit user tracking at this stage.
/// </summary>
public abstract class AggregateRoot : BaseEntity, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
