using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ECommerce.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request execution time.
/// Warns when a request exceeds the slow-request threshold (500 ms).
/// </summary>
public sealed class LoggingPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;

    public LoggingPipelineBehavior(ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {RequestName} took {ElapsedMs} ms.",
                requestName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs} ms.",
                requestName, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
