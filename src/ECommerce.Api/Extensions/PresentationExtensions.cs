using ECommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Api.Extensions;

/// <summary>
/// Extension methods for registering Presentation-layer services
/// (controllers, Swagger, CORS, health checks, ProblemDetails).
/// </summary>
public static class PresentationExtensions
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // ── File Upload Limits ─────────────────────────────────────────────────
        // Default Kestrel multipart body limit is 128MB; we cap it at 10MB to match
        // validator rules (5MB per file + overhead). Prevents memory pressure from
        // oversized requests before they reach the validator.
        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
            options.ValueLengthLimit         = int.MaxValue;
            options.MultipartHeadersLengthLimit = 16384;         // 16 KB for headers
        });

        services.AddSwaggerWithJwt();

        services.AddCorsPolicy(configuration);

        // RFC 7807 ProblemDetails for consistent error responses
        services.AddProblemDetails();

        // ── Rate Limiting ──────────────────────────────────────────────────────
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth-policy", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        SegmentsPerWindow = 3,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
            
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // Health checks — adding DbContext and Npgsql checks
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>(name: "database");

        // ── Hangfire Background Jobs ───────────────────────────────────────────
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => 
                options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))
            ));

        services.AddHangfireServer();

        return services;
    }

    public static WebApplication MapPresentationEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHealthChecks("/health");

        return app;
    }
}
