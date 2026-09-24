using MediatR;

namespace ECommerce.Domain.Abstractions;

/// <summary>
/// Marker interface for domain events. Implements MediatR INotification
/// so events can be dispatched via the mediator pipeline.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
}
