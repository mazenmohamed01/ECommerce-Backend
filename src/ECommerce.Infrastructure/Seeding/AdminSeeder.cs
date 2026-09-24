using ECommerce.Domain.Constants;
using ECommerce.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Seeding;

/// <summary>
/// Idempotent startup seeder that ensures the Admin role and a default Admin account exist.
/// Safe to call on every application startup — it performs existence checks before any write.
/// Credentials are read from IConfiguration (appsettings.json → "AdminSeed" section).
/// </summary>
public static class AdminSeeder
{
    /// <summary>
    /// Seeds the Admin role and default admin user if they do not already exist.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager   = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager   = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config        = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger        = loggerFactory.CreateLogger("AdminSeeder");

        // ── Read seed configuration ────────────────────────────────────────────
        var seedSection = config.GetSection("AdminSeed");
        var fullName    = seedSection["FullName"] ?? "System Administrator";
        var email       = seedSection["Email"];
        var password    = seedSection["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "AdminSeed configuration is missing Email or Password. Admin seeding skipped.");
            return;
        }

        // ── Ensure Admin role exists ───────────────────────────────────────────
        if (!await roleManager.RoleExistsAsync(Roles.Admin))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(Roles.Admin));
            if (roleResult.Succeeded)
                logger.LogInformation("Created '{Role}' role.", Roles.Admin);
            else
                logger.LogError("Failed to create '{Role}' role: {Errors}",
                    Roles.Admin,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        // ── Ensure Customer role exists (needed for customer registration flow) ─
        if (!await roleManager.RoleExistsAsync(Roles.Customer))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(Roles.Customer));
            if (roleResult.Succeeded)
                logger.LogInformation("Created '{Role}' role.", Roles.Customer);
            else
                logger.LogError("Failed to create '{Role}' role: {Errors}",
                    Roles.Customer,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        // ── Ensure default Admin user exists ───────────────────────────────────
        var existingAdmin = await userManager.FindByEmailAsync(email);
        if (existingAdmin is not null)
        {
            logger.LogDebug("Admin account '{Email}' already exists. Seeding skipped.", email);
            return;
        }

        // Parse FullName into first and last
        var nameParts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : "System";
        var lastName  = nameParts.Length > 1 ? nameParts[1] : "Administrator";

        var adminUser = new ApplicationUser
        {
            UserName       = email,
            Email          = email,
            FirstName      = firstName,
            LastName       = lastName,
            EmailConfirmed = true,
            CreatedAt      = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(adminUser, password);
        if (!createResult.Succeeded)
        {
            logger.LogError("Admin seeding failed for '{Email}': {Errors}",
                email,
                string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
        logger.LogInformation("Default admin account '{Email}' seeded successfully.", email);
    }
}
