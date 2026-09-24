using MediatR;

namespace ECommerce.Application.Abstractions;

/// <summary>
/// Marker interface for a CQRS Query (read operation).
/// Handlers for queries must not produce side effects.
/// </summary>
/// <typeparam name="TResponse">The query result type.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>;
