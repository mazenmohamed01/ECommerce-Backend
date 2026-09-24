using ECommerce.Application.Interfaces;
using ECommerce.Domain.Interfaces;
using ECommerce.Infrastructure.Authentication;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Identity;
using ECommerce.Infrastructure.Persistence;
using ECommerce.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ECommerce.Infrastructure.DependencyInjection;

/// <summary>
/// Registers all Infrastructure-layer services.
/// Called from Api via IServiceCollection.AddInfrastructure().
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddIdentityServices()
            .AddJwtAuthentication(configuration)
            .AddGoogleSettings(configuration)
            .AddAuthorizationPolicies()
            .AddRepositories()
            .AddCloudinary(configuration)
            .AddRedisCache(configuration)
            .AddMoyasarSettings(configuration)
            .AddEmailService(configuration)
            .AddMoyasarSettings(configuration)
            .AddN8nIntegration(configuration);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(InfrastructureServiceExtensions).Assembly));

        return services;
    }

    // -------------------------------------------------------------------------
    // Private helpers — each concern is isolated in its own method
    // -------------------------------------------------------------------------

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found in configuration.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            }));

        return services;
    }

    private static IServiceCollection AddIdentityServices(
        this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromMinutes(15));

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);
        
        services.AddScoped<ECommerce.Application.Interfaces.IJwtProvider, JwtProvider>();

        var jwtSettings = jwtSection.Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT settings are not configured.");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        return services;
    }

    private static IServiceCollection AddAuthorizationPolicies(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole(ECommerce.Domain.Constants.Roles.Admin));

            options.AddPolicy("CustomerOrAdmin", policy =>
                policy.RequireRole(
                    ECommerce.Domain.Constants.Roles.Customer,
                    ECommerce.Domain.Constants.Roles.Admin));
        });

        return services;
    }

    private static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Phase 1 — Catalog repositories ─────────────────────────────────────
        services.AddScoped<ICategoryRepository,    CategoryRepository>();
        services.AddScoped<IProductRepository,     ProductRepository>();
        services.AddScoped<IProductImageRepository, ProductImageRepository>();
        services.AddScoped<ICartRepository,         CartRepository>();
        services.AddScoped<IOrderRepository,        OrderRepository>();
        services.AddScoped<IProcessedWebhookEventRepository, ProcessedWebhookEventRepository>();
        services.AddScoped<IOutboundWebhookEventRepository, OutboundWebhookEventRepository>();
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();

        return services;
    }

    private static IServiceCollection AddCloudinary(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind and validate Cloudinary settings at startup
        services.Configure<CloudinarySettings>(
            configuration.GetSection(CloudinarySettings.SectionName));

        // Register the service — Scoped so it participates in the request lifecycle
        services.AddScoped<ICloudinaryService, CloudinaryService>();

        return services;
    }

    private static IServiceCollection AddGoogleSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GoogleSettings>(
            configuration.GetSection(GoogleSettings.SectionName));

        return services;
    }

    private static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("RedisConnection")
            ?? throw new InvalidOperationException("Connection string 'RedisConnection' not found in configuration.");

        services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
            StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));

        services.AddSingleton<IOrderNumberGenerator, RedisOrderNumberGenerator>();

        return services;
    }

    private static IServiceCollection AddMoyasarSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MoyasarSettings>()
            .Bind(configuration.GetSection(MoyasarSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IPaymentService, MoyasarPaymentService>((provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<MoyasarSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl);
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.ApiKey}:"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        })
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
        .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

        services.AddHttpClient("OutboundWebhookClient")
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt))))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15)));

        return services;
    }

    private static IServiceCollection AddEmailService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ECommerce.Infrastructure.Configurations.EmailSettings>(
            configuration.GetSection("EmailSettings"));

        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    private static IServiceCollection AddN8nIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<N8nSettings>(
            configuration.GetSection(N8nSettings.SectionName));

        services.AddHttpClient<IN8nIntegrationService, N8nIntegrationService>()
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(2, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
            .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)));

        return services;
    }
}
