using ECommerce.Api.Middlewares;

namespace ECommerce.Api.Extensions;

/// <summary>
/// Extension method for registering the global exception middleware.
/// </summary>
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(
        this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
