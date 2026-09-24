using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

/// <summary>
/// Single application DbContext extending IdentityDbContext so domain entities
/// and Identity tables share the same database and can participate in the same transaction.
///
/// All entity configurations are discovered automatically via ApplyConfigurationsFromAssembly —
/// no Fluent API calls belong in this file.
/// </summary>
public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ── Domain DbSets ─────────────────────────────────────────────────────────
    public DbSet<Category>     Categories  { get; init; }
    public DbSet<Product>      Products    { get; init; }
    public DbSet<ProductImage> ProductImages { get; init; }
    public DbSet<Cart>         Carts       { get; init; }
    public DbSet<CartItem>     CartItems   { get; init; }
    public DbSet<Order>        Orders      { get; init; }
    public DbSet<OrderItem>    OrderItems  { get; init; }
    public DbSet<UserAddress>  UserAddresses { get; init; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; init; }
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; init; }
    public DbSet<OutboundWebhookEndpoint> OutboundWebhookEndpoints { get; init; }
    public DbSet<OutboundWebhookEvent> OutboundWebhookEvents { get; init; }

    // ── Identity DbSets ───────────────────────────────────────────────────────
    public DbSet<RefreshToken> RefreshTokens { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Discover and apply all IEntityTypeConfiguration<T> in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // Hook point: dispatch domain events before persisting.
        // Implement a domain event dispatcher interceptor here when needed.
        return await base.SaveChangesAsync(cancellationToken);
    }
}
