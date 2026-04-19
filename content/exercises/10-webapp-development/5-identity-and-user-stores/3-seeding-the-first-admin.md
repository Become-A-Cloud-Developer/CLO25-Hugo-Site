+++
title = "3. Seeding the First Admin"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Replace the hardcoded test seeder with a config-driven, idempotent admin bootstrap that works for both InMemory and SQLite"
weight = 3
draft = false
+++

# Seeding the First Admin

## Goal

A fresh database has no users. In Exercise 5.1 we cheated by hardcoding test credentials in `TestUserSeeder`. This exercise replaces that with the **real-world pattern**: the admin user's details live in configuration (typically `user-secrets` or environment variables), the seeder reads them on every boot, and it's idempotent — creating the admin on a fresh DB and doing nothing on subsequent restarts.

The same code path handles both stores:
- **InMemory**: the admin user disappears every restart; the seeder recreates from config.
- **SQLite**: the admin user persists; the seeder sees the existing row and takes no action.

Same idempotent check, different persistence behavior. That's the point.

> **What you'll learn:**
>
> - Why public "bootstrap" endpoints are dangerous and why config-driven seeding is the right pattern
> - How `UserManager.FindByNameAsync` makes seeding idempotent
> - Why `user-secrets` is the right place for development admin credentials
> - How `IHostEnvironment.IsDevelopment()` lets you mix production patterns with dev conveniences

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed Exercises 5.1 and 5.2 — Identity with both stores works
> - ✓ The diagnostics page correctly reports the active provider

## Exercise Steps

### Overview

1. **Write** an `IdentitySeeder` that replaces `TestUserSeeder`
2. **Move** admin credentials out of code and into configuration
3. **Keep** the dev-only candidate user for lab convenience
4. **Switch** `Program.cs` to call the new seeder
5. **Test Your Implementation** — including the real `user-secrets` pattern

### **Step 1:** Write IdentitySeeder

The seeder does three things:
1. Ensures the `Admin` and `Candidate` roles exist
2. Creates the admin user from `AdminSeed:*` config — or logs a warning if not configured
3. In Development only, creates a convenience candidate user

1. **Create** `src/CloudSoft.Auth.Web/Data/IdentitySeeder.cs`:

   > `src/CloudSoft.Auth.Web/Data/IdentitySeeder.cs`

   ```csharp
   using System.Security.Claims;
   using Microsoft.AspNetCore.Identity;

   namespace CloudSoft.Auth.Web.Data;

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
                   await roleManager.CreateAsync(new IdentityRole(role));
           }

           await SeedAdminFromConfigAsync(userManager, config, logger);

           if (env.IsDevelopment())
               await SeedDevCandidateAsync(userManager);
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
               return; // idempotent

           var admin = new ApplicationUser
           {
               UserName = username,
               Email = email,
               EmailConfirmed = true,
           };

           var create = await userManager.CreateAsync(admin, password);
           if (!create.Succeeded)
               throw new InvalidOperationException(
                   $"Failed to seed admin '{username}': " +
                   string.Join("; ", create.Errors.Select(e => e.Description)));

           await userManager.AddToRoleAsync(admin, "Admin");
           await userManager.AddClaimAsync(admin, new Claim("Department", department));

           logger.LogInformation("Seeded admin user '{Username}' from AdminSeed config.", username);
       }

       private static async Task SeedDevCandidateAsync(UserManager<ApplicationUser> userManager)
       {
           const string username = "candidate";
           if (await userManager.FindByNameAsync(username) is not null) return;

           var candidate = new ApplicationUser
           {
               UserName = username,
               Email = "candidate@example.com",
               EmailConfirmed = true,
           };

           var create = await userManager.CreateAsync(candidate, "candidate");
           if (!create.Succeeded) return;

           await userManager.AddToRoleAsync(candidate, "Candidate");
           await userManager.AddClaimAsync(candidate, new Claim("Department", "Sales"));
       }
   }
   ```

### **Step 2:** Point Program.cs at the new seeder

1. **Open** `src/CloudSoft.Auth.Web/Program.cs`

2. **Replace** the `TestUserSeeder.SeedAsync(...)` call with:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   await IdentitySeeder.SeedAsync(app.Services, app.Environment);
   ```

3. **Delete** the old `Data/TestUserSeeder.cs` — it's dead code.

### **Step 3:** Provide dev defaults, prepare for prod

Production credentials never live in `appsettings.json`. Development convenience values can.

1. **Edit** `src/CloudSoft.Auth.Web/appsettings.Development.json`:

   > `src/CloudSoft.Auth.Web/appsettings.Development.json`

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AdminSeed": {
       "Username": "admin",
       "Email": "admin@example.com",
       "Password": "admin",
       "Department": "Engineering"
     }
   }
   ```

2. `appsettings.json` stays clean — no `AdminSeed` section. Production configuration comes from a different source.

### **Step 4:** Configure user-secrets (the production-quality pattern)

Even in dev, `user-secrets` is how most real teams store secrets. The file lives **outside** the repo under `~/.microsoft/usersecrets/…/secrets.json` and is never checked in.

```bash
cd src/CloudSoft.Auth.Web
dotnet user-secrets init                                              # once per project
dotnet user-secrets set "AdminSeed:Username" "admin"
dotnet user-secrets set "AdminSeed:Email"    "admin@example.com"
dotnet user-secrets set "AdminSeed:Password" "ChangeMe-in-prod"
```

With `user-secrets` set, you can remove `AdminSeed` from `appsettings.Development.json` and the seeder still finds the values. The configuration system merges secrets on top of JSON configuration during development.

In production (Azure, Kubernetes, anywhere), the same keys come from environment variables (`AdminSeed__Username=...`) or a secret store (Azure Key Vault).

### **Step 5:** Test Your Implementation

**InMemory (default):**
1. `dotnet run` — the admin is seeded on every boot.
2. Log in as `admin / admin`. WhoAmI shows role + Department claim.
3. Stop, restart. Seeder runs again against an empty store; admin is recreated.

**SQLite:**
1. Wipe any existing DB: `rm src/CloudSoft.Auth.Web/cloudsoft-auth.db*`
2. `IdentityStore__Provider=SQLite dotnet run`
3. First boot: seeder creates admin in the new DB.
4. Stop, start again. The seeder's `FindByNameAsync` returns the existing admin; no action taken. Login still works.

**Missing config:**
1. Remove or rename the `AdminSeed` section.
2. Start the app — a `WARN` log entry appears explaining what's missing.
3. Admin doesn't exist; `/Account/Login` with admin credentials fails.

> ✓ **Success indicators:**
>
> - Admin login works under InMemory and SQLite
> - SQLite persists the admin across restarts
> - Missing `AdminSeed` produces a warning instead of a crash
> - `dotnet user-secrets list` shows the keys your seeder reads

## Common Issues

> **If you encounter problems:**
>
> **"Failed to seed admin: PasswordTooShort":** The password doesn't meet the configured policy. Either relax `PasswordOptions` (we already did, back in Exercise 5.1) or use a stronger seed password.
>
> **`user-secrets` values not picked up:** Confirm `dotnet user-secrets list` shows the keys. The `UserSecretsId` in the `.csproj` must exist — `dotnet user-secrets init` generates and writes it.
>
> **Admin created twice with different passwords:** A stale `FindByNameAsync` result or race across boots. In a single-node dev app this shouldn't happen; if it does, the idempotency check is wrong.

## Summary

The admin bootstrap is now production-shaped:

- ✓ Admin credentials live in configuration, not source
- ✓ `user-secrets` (dev) / env vars (prod) handle secret material
- ✓ The seeder is idempotent — works on fresh and existing databases
- ✓ The dev candidate user stays a dev convenience, isolated behind `IHostEnvironment.IsDevelopment()`

> **Key takeaway:** "How does the first privileged user get into the system?" is a real operations question. The answer in almost every real app is config-driven idempotent startup seeding — exactly what you've built.

## Done!

Admin exists on every boot without any bypass endpoint. The final exercise — **Registration and Role Promotion** — completes the picture: ordinary users self-register as Candidates, and admins promote them when appropriate.
