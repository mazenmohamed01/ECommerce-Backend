 using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
namespace ECommerce.Api.Middlewares;

/// <summary>
/// Global exception handling middleware.
/// Converts unhandled exceptions into RFC 7807 ProblemDetails responses.
/// Keeps controller actions clean — they only handle the happy path.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            ValidationException validation =>
                (StatusCodes.Status400BadRequest, "Validation Failed",
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))),

            UnauthorizedAccessException =>
                (StatusCodes.Status401Unauthorized, "Unauthorized", "You are not authorized to perform this action."),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error",
                    _environment.IsDevelopment() ? exception.ToString() : "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        // Attach validation errors as an extension
        if (exception is ValidationException validationEx)
        {
            problemDetails.Extensions["errors"] = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, JsonOptions));
    }
}
