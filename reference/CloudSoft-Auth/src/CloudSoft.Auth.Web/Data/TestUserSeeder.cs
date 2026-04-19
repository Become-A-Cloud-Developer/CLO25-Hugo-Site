using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace CloudSoft.Auth.Web.Data;

/// <summary>
/// Seeds a small fixed set of users for the exercises. Exercise 5.3 replaces
/// this with a config-driven admin seeder; until then this is enough to let
/// the exercises demonstrate Identity with realistic, role-scoped accounts.
/// </summary>
public static class TestUserSeeder
{
    private record SeedUser(string Username, string Email, string Password, string Role, string Department);

    private static readonly SeedUser[] Seeds =
    [
        new("admin",     "admin@example.com",     "admin",     "Admin",     "Engineering"),
        new("candidate", "candidate@example.com", "candidate", "Candidate", "Sales"),
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in new[] { "Admin", "Candidate" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        foreach (var seed in Seeds)
        {
            var user = await userManager.FindByNameAsync(seed.Username);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = seed.Username,
                    Email = seed.Email,
                    EmailConfirmed = true,
                };
                var create = await userManager.CreateAsync(user, seed.Password);
                if (!create.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Failed to seed {seed.Username}: {string.Join("; ", create.Errors.Select(e => e.Description))}");
                }
                await userManager.AddToRoleAsync(user, seed.Role);
                await userManager.AddClaimAsync(user, new Claim("Department", seed.Department));
            }
        }
    }
}
