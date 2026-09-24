using ECommerce.Application.Behaviors;
using ECommerce.Application.Interfaces;
using ECommerce.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ECommerce.Application.DependencyInjection;

/// <summary>
/// Registers all Application-layer services.
/// Called from Api via IServiceCollection.AddApplication().
/// </summary>
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR — registers handlers, pipeline behaviors in declared order
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Behaviors execute in registration order (logging → validation → handler)
            cfg.AddOpenBehavior(typeof(LoggingPipelineBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        // FluentValidation — scan and register all validators in this assembly
        services.AddValidatorsFromAssembly(assembly);

        // AutoMapper — scan profiles in this assembly
        services.AddAutoMapper(cfg => cfg.AddMaps(assembly));

        // ── Phase 1 — Catalog services ─────────────────────────────────────────
        // Singleton: stateless pure service, no I/O or captured scope
        services.AddSingleton<ISlugService, SlugService>();

        services.AddScoped<IStockService,       StockService>();

        // ── Authentication services ────────────────────────────────────────────
        // Registered here as interface stubs; implementations live in Infrastructure
        // (registered via AddInfrastructure) to keep Application free of Identity deps.

        return services;
    }
}
