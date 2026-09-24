using Microsoft.EntityFrameworkCore;
using ECommerce.Application.DependencyInjection;
using ECommerce.Api.Extensions;
using ECommerce.Infrastructure.DependencyInjection;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Seeding;
using Serilog;
using Hangfire;

// ─── Bootstrap Serilog from configuration before the host is built ───────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting ECommerce API...");

    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, configuration) =>
        configuration
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    // ─── Service Registration ─────────────────────────────────────────────────
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddPresentation(builder.Configuration);

    var app = builder.Build();

    // ─── Auto Migrations & Startup Seeding ─────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    // Idempotent: checks existence before creating roles or the admin account.
    await AdminSeeder.SeedAsync(app.Services);

    // ─── Middleware Pipeline ───────────────────────────────────────────────────
    app.UseGlobalExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerWithUi();
    }

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors(CorsExtensions.PolicyName);
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapPresentationEndpoints();

    // ─── Hangfire Dashboard & Jobs ─────────────────────────────────────────────
    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        // For production, configure authorization rules here
        Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
    });

    // Schedule the recurring job every minute (it checks for orders older than 15 mins)
    Hangfire.RecurringJob.AddOrUpdate<ECommerce.Application.BackgroundJobs.OrderExpiryJob>(
        "order-expiry-job",
        job => job.ExecuteAsync(CancellationToken.None),
        Hangfire.Cron.Minutely);

    // Schedule the outbound webhook processor
    Hangfire.RecurringJob.AddOrUpdate<ECommerce.Application.BackgroundJobs.OutboundWebhookJob>(
        "outbound-webhook-job",
        job => job.ExecuteAsync(CancellationToken.None),
        Hangfire.Cron.Minutely);

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
