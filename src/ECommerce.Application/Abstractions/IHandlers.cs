using MediatR;

namespace ECommerce.Application.Abstractions;

/// <summary>Handler contract for commands that return a value.</summary>
public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;

/// <summary>Handler contract for fire-and-forget commands.</summary>
public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand>
    where TCommand : ICommand;

/// <summary>Handler contract for queries.</summary>
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;
