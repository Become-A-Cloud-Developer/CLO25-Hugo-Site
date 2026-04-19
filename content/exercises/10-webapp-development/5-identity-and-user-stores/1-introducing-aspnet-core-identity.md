+++
title = "1. Introducing ASP.NET Core Identity"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Replace the hand-rolled cookie sign-in from Chapter 4 with ASP.NET Core Identity backed by EF Core InMemory. Same user experience, managed user store."
weight = 1
draft = false
+++

# Introducing ASP.NET Core Identity

## Goal

Replace the hand-rolled authentication stack from Chapter 4 with **ASP.NET Core Identity**. Identity provides `UserManager`, `RoleManager`, and `SignInManager` — high-level APIs that handle user persistence, password hashing, role assignment, and the cookie round-trip. The Who Am I page will look almost identical; only the scheme name (`Identity.Application`) and the origin of the claims will change.

To avoid introducing a database too early, we use **EF Core InMemory** as the store. Users and roles live in the process only — every restart wipes them, and a seed method recreates them. Exercise 5.2 introduces a feature flag that swaps this for SQLite persistence.

> **What you'll learn:**
>
> - How `IdentityUser`, `IdentityRole`, and `IdentityDbContext` fit together
> - How to register Identity with DI and tell it which store to use
> - How `SignInManager.PasswordSignInAsync` replaces the hand-built `ClaimsPrincipal` + `SignInAsync`
> - How role and claim data flow through Identity's user-roles and user-claims tables into the cookie

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed Chapter 4 — the cookie flow, roles, claims, and policies work end-to-end
> - ✓ `.NET` 10 SDK installed
> - ✓ The project builds and tests pass at the end of Exercise 4.4

## Exercise Steps

### Overview

1. **Install** the Identity + EF Core packages
2. **Create** `ApplicationUser` and `ApplicationDbContext`
3. **Register** Identity in `Program.cs` with the InMemory provider
4. **Replace** `AccountController`'s hand-rolled logic with `SignInManager`
5. **Seed** the fixed test users, roles, and claims on startup
6. **Delete** the old `DummyUsers.cs`
7. **Test Your Implementation**

### **Step 1:** Install the Identity and EF Core packages

```bash
dotnet add src/CloudSoft.Auth.Web package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add src/CloudSoft.Auth.Web package Microsoft.EntityFrameworkCore.InMemory
```

### **Step 2:** Create the application user and database context

Identity is designed to be extended. `IdentityUser` gives you the standard columns (id, username, email, password hash, security stamp); you subclass it to add your own. For this chapter the empty subclass is enough.

1. **Create** `src/CloudSoft.Auth.Web/Data/ApplicationUser.cs`:

   > `src/CloudSoft.Auth.Web/Data/ApplicationUser.cs`

   ```csharp
   using Microsoft.AspNetCore.Identity;

   namespace CloudSoft.Auth.Web.Data;

   public class ApplicationUser : IdentityUser
   {
   }
   ```

2. **Create** `src/CloudSoft.Auth.Web/Data/ApplicationDbContext.cs`:

   > `src/CloudSoft.Auth.Web/Data/ApplicationDbContext.cs`

   ```csharp
   using Microsoft.AspNetCore.Identity;
   using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
   using Microsoft.EntityFrameworkCore;

   namespace CloudSoft.Auth.Web.Data;

   public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
   {
       public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
       {
       }
   }
   ```

> ℹ **Concept Deep Dive**
>
> `IdentityDbContext<TUser, TRole, TKey>` wires up the standard Identity tables: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, and a few more. You don't write any of these — they come from Identity's `OnModelCreating`.

### **Step 3:** Register Identity with DI

The registration in `Program.cs` replaces most of your Chapter 4 authentication code. Identity installs its own cookie scheme (`Identity.Application`) with sensible defaults; the `ConfigureApplicationCookie` call just tweaks the paths you care about.

1. **Open** `src/CloudSoft.Auth.Web/Program.cs`

2. **Replace** the old `AddAuthentication(Cookies).AddCookie(...)` block with:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   using CloudSoft.Auth.Web.Data;
   using Microsoft.AspNetCore.Identity;
   using Microsoft.EntityFrameworkCore;

   // Data: EF Core InMemory store backing ASP.NET Core Identity.
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
       options.UseInMemoryDatabase("CloudSoftAuthDb"));

   builder.Services
       .AddIdentity<ApplicationUser, IdentityRole>(options =>
       {
           // Relax password rules for the lab: Chapter-4 credentials still work.
           options.Password.RequireDigit = false;
           options.Password.RequireLowercase = false;
           options.Password.RequireUppercase = false;
           options.Password.RequireNonAlphanumeric = false;
           options.Password.RequiredLength = 3;
           options.User.RequireUniqueEmail = false;
           options.SignIn.RequireConfirmedAccount = false;
       })
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();

   builder.Services.ConfigureApplicationCookie(options =>
   {
       options.LoginPath = "/Account/Login";
       options.AccessDeniedPath = "/Account/AccessDenied";
   });
   ```

3. **Change** Google registration to attach to the existing scheme:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
   var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
   if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
   {
       builder.Services.AddAuthentication().AddGoogle(options =>
       {
           options.ClientId = googleClientId;
           options.ClientSecret = googleClientSecret;
       });
   }
   ```

> ⚠ **Common Mistakes**
>
> - Calling `AddAuthentication(scheme).AddCookie()` alongside `AddIdentity` clobbers Identity's cookie configuration. `AddIdentity` already sets up the scheme; you only configure it.
> - Forgetting `AddDefaultTokenProviders()` breaks password reset and email confirmation (not used in this lab, but needed in Ex 5.4).

### **Step 4:** Replace AccountController with SignInManager

The controller no longer builds a `ClaimsPrincipal` by hand. `SignInManager.PasswordSignInAsync` takes a username and password, looks up the user, verifies the hash, and issues the cookie — all at once.

1. **Replace** `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`:

   > `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

   ```csharp
   using CloudSoft.Auth.Web.Data;
   using Microsoft.AspNetCore.Authentication;
   using Microsoft.AspNetCore.Authorization;
   using Microsoft.AspNetCore.Identity;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudSoft.Auth.Web.Controllers;

   public class AccountController : Controller
   {
       private readonly SignInManager<ApplicationUser> _signInManager;

       public AccountController(SignInManager<ApplicationUser> signInManager)
       {
           _signInManager = signInManager;
       }

       [HttpGet]
       public IActionResult Login(string? returnUrl = null)
       {
           ViewData["ReturnUrl"] = returnUrl;
           return View();
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
       {
           var result = await _signInManager.PasswordSignInAsync(
               username, password, isPersistent: false, lockoutOnFailure: false);

           if (!result.Succeeded)
           {
               ModelState.AddModelError(string.Empty, "Invalid username or password.");
               ViewData["ReturnUrl"] = returnUrl;
               return View();
           }

           if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
           {
               return Redirect(returnUrl);
           }

           return RedirectToAction("Index", "WhoAmI");
       }

       [HttpPost]
       [ValidateAntiForgeryToken]
       public async Task<IActionResult> Logout()
       {
           await _signInManager.SignOutAsync();
           return RedirectToAction("Index", "Home");
       }

       [HttpGet]
       public IActionResult AccessDenied() => View();

       [HttpPost]
       [AllowAnonymous]
       [ValidateAntiForgeryToken]
       public IActionResult ExternalLogin(string provider, string? returnUrl = null)
       {
           var redirectUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
               ? returnUrl
               : Url.Action("Index", "WhoAmI");

           var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
           return Challenge(properties, provider);
       }
   }
   ```

> ℹ **Concept Deep Dive**
>
> `SignInManager` sits on top of `UserManager`. Internally it calls `UserManager.FindByNameAsync`, verifies the password hash, and issues the `Identity.Application` cookie via `SignInAsync` on `HttpContext`. The claims that end up in the cookie are assembled by a `IUserClaimsPrincipalFactory` which, by default, includes the user's roles and all `AspNetUserClaims` rows.

### **Step 5:** Seed fixed test users at startup

InMemory storage means every restart starts with zero users. A small seeder populates admin and candidate on every boot. Exercise 5.3 replaces this with a config-driven seeder that reads the admin credentials from `user-secrets`; for now, hardcoded is fine.

1. **Create** `src/CloudSoft.Auth.Web/Data/TestUserSeeder.cs`:

   > `src/CloudSoft.Auth.Web/Data/TestUserSeeder.cs`

   ```csharp
   using System.Security.Claims;
   using Microsoft.AspNetCore.Identity;

   namespace CloudSoft.Auth.Web.Data;

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
                   await roleManager.CreateAsync(new IdentityRole(role));
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
   ```

2. **Call** the seeder from `Program.cs`, just before `app.Run()`:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   await TestUserSeeder.SeedAsync(app.Services);

   app.Run();
   ```

> ℹ **Concept Deep Dive**
>
> The seeder is idempotent because it checks `FindByNameAsync` before creating. With InMemory the check always returns null on the first boot and the user is created; with SQLite (Exercise 5.2) the user is created once and every subsequent boot finds them. Same code, different persistence.

### **Step 6:** Delete the old DummyUsers.cs

`AccountController` no longer references `DummyUsers`. The class is dead code.

```bash
rm src/CloudSoft.Auth.Web/Data/DummyUsers.cs
```

### **Step 7:** Test Your Implementation

1. **Run** the app:

   ```bash
   dotnet run --project src/CloudSoft.Auth.Web --launch-profile http
   ```

2. Sign in as `admin` / `admin`. The Who Am I page now shows:
   - `Authentication type: Identity.Application` (was `Cookies` in Chapter 4)
   - `Roles: Admin` (from Identity's user-roles join)
   - Claims table includes the role claim and `Department = Engineering`

3. The admin-only page and engineering page still work exactly as before.

4. Restart the app (Ctrl+C, `dotnet run` again). Log in as admin again. This proves the seeder re-runs on every InMemory boot.

> ✓ **Success indicators:**
>
> - Login still works with the Chapter-4 credentials
> - The authentication scheme name changed to `Identity.Application`
> - Roles and claims surface through Identity rather than being assembled by hand
> - The seeder runs cleanly; a second login attempt doesn't create duplicate users

## Common Issues

> **If you encounter problems:**
>
> **PasswordValidator error when seeding:** The password doesn't meet the configured policy. Either loosen `PasswordOptions` further (for dev only) or use stronger seed passwords.
>
> **"Scheme Cookies does not exist":** You removed `AddCookie(...)` but something (old code) still references the `Cookies` scheme name. Identity's scheme is `IdentityConstants.ApplicationScheme` (`"Identity.Application"`).
>
> **Google button disappeared:** The external-schemes query in `Login.cshtml` also returned the classic cookie scheme pre-Identity (no-op because it has no `DisplayName`). Identity doesn't set a `DisplayName` on its cookie either, so the filter still works. If you see nothing after configuring Google, check your user-secrets.

## Summary

You've migrated from a hand-rolled cookie sign-in to Identity-managed authentication:

- ✓ `ApplicationUser` + `ApplicationDbContext` as the data model
- ✓ `AddIdentity` + `AddEntityFrameworkStores` for the plumbing
- ✓ `SignInManager.PasswordSignInAsync` replaces manual `ClaimsPrincipal` construction
- ✓ A startup seed populates the same two test users the lab has used all along

> **Key takeaway:** Identity is a thin abstraction over the primitives you built in Chapter 4. The cookie is the same kind of cookie; the claims are the same kind of claims. The gain is persistence-ready user management for free.

## Done!

Users live in memory. **Exercise 5.2** adds a feature flag that lets you swap the store for SQLite without changing any other code.
