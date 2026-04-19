using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace CloudSoft.Auth.Web.Data;

/// <summary>
/// Idempotent startup seeder that works for both the InMemory and SQLite
/// stores. On each boot it:
///
/// 1. Ensures the Admin and Candidate roles exist.
/// 2. Bootstraps the admin user from configuration (AdminSeed:Username,
///    AdminSeed:Email, AdminSeed:Password — typically provided via
///    dotnet user-secrets or environment variables in production).
/// 3. In Development only, seeds a convenience candidate user for the labs.
///
/// For InMemory all of this runs every boot. For SQLite it runs only on
/// fresh databases; subsequent boots find existing rows and take no action.
/// </summary>
public static class IdentitySeeder
{
    private static readonly string[] RequiredRoles = ["Admin", "Candidate"];

    public static async Task SeedAsync(IServiceProvider services, IHostEnvironment env)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        foreach (var role in RequiredRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await SeedAdminFromConfigAsync(userManager, config, logger);

        if (env.IsDevelopment())
        {
            await SeedDevCandidateAsync(userManager);
        }
    }

    private static async Task SeedAdminFromConfigAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        ILogger logger)
    {
        var username = config["AdminSeed:Username"];
        var email    = config["AdminSeed:Email"];
        var password = config["AdminSeed:Password"];
        var department = config["AdminSeed:Department"] ?? "Engineering";

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "AdminSeed is not fully configured. Set AdminSeed:Username, " +
                "AdminSeed:Email, and AdminSeed:Password via user-secrets or " +
                "environment variables to enable admin bootstrap.");
            return;
        }

        if (await userManager.FindByNameAsync(username) is not null)
        {
            return; // idempotent: admin already seeded
        }

        var admin = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
        };

        var create = await userManager.CreateAsync(admin, password);
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed admin '{username}': " +
                string.Join("; ", create.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, "Admin");
        await userManager.AddClaimAsync(admin, new Claim("Department", department));

        logger.LogInformation("Seeded admin user '{Username}' from AdminSeed config.", username);
    }

    private static async Task SeedDevCandidateAsync(UserManager<ApplicationUser> userManager)
    {
        const string username = "candidate";
        if (await userManager.FindByNameAsync(username) is not null)
        {
            return;
        }

        var candidate = new ApplicationUser
        {
            UserName = username,
            Email = "candidate@example.com",
            EmailConfirmed = true,
        };

        var create = await userManager.CreateAsync(candidate, "candidate");
        if (!create.Succeeded)
        {
            return; // dev convenience — silently skip if the policy rejects it
        }

        await userManager.AddToRoleAsync(candidate, "Candidate");
        await userManager.AddClaimAsync(candidate, new Claim("Department", "Sales"));
    }
}
