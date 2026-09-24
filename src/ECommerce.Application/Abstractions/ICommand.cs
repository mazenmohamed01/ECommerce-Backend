using MediatR;

namespace ECommerce.Application.Abstractions;

/// <summary>
/// Marker interface for a CQRS Command (write operation).
/// Implement this with the response type your handler returns.
/// </summary>
/// <typeparam name="TResponse">The handler return type (e.g. Result, Guid).</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>;

/// <summary>Marker interface for a fire-and-forget command with no return value.</summary>
public interface ICommand : IRequest;
