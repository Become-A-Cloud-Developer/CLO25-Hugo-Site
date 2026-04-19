+++
title = "2. User Store: InMemory vs SQLite with a Feature Flag"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Let a single configuration flag choose between EF Core InMemory (transient) and SQLite (persistent) storage for the Identity user store"
weight = 2
draft = false
+++

# User Store: InMemory vs SQLite with a Feature Flag

## Goal

The Identity integration from Exercise 5.1 uses EF Core InMemory — fast, zero setup, but volatile. Real applications need persistence. Instead of committing to one or the other, **a feature flag in configuration chooses at startup**. The rest of the code doesn't change.

This teaches the broader pattern: when a component has a clean interface (here, `DbContext`), swapping implementations is a DI registration change and nothing else. The same `UserManager`, the same `SignInManager`, the same views — two different storage layers.

> **What you'll learn:**
>
> - How `AddDbContext` is the seam where you swap EF Core providers
> - How configuration (flags and connection strings) controls runtime behavior
> - When `EnsureCreated()` is appropriate versus EF Core migrations
> - How to verify a feature flag is actually in effect at runtime

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed Exercise 5.1 — Identity with InMemory works
> - ✓ The Chapter-4 regression tests still pass

## Exercise Steps

### Overview

1. **Install** the SQLite EF Core provider
2. **Add** the `IdentityStore:Provider` flag to `appsettings.json`
3. **Swap** `AddDbContext` to a `switch` on the provider name
4. **Call** `EnsureCreated()` for relational providers
5. **Build** a `/Diagnostics/Store` page that reports the active provider
6. **Test Your Implementation** under both stores

### **Step 1:** Install the SQLite provider

```bash
dotnet add src/CloudSoft.Auth.Web package Microsoft.EntityFrameworkCore.Sqlite
```

### **Step 2:** Configure the flag and the connection string

Configuration settings are the right home for "what provider?" and "where's the file?" — the application code stays free of literals.

1. **Edit** `src/CloudSoft.Auth.Web/appsettings.json`:

   > `src/CloudSoft.Auth.Web/appsettings.json`

   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "*",
     "IdentityStore": {
       "Provider": "InMemory"
     },
     "ConnectionStrings": {
       "Identity": "Data Source=cloudsoft-auth.db"
     }
   }
   ```

> ℹ **Concept Deep Dive**
>
> Configuration keys with colons (`IdentityStore:Provider`) map to nested objects in JSON. The same key can be overridden via environment variable: `IdentityStore__Provider=SQLite` (`__` in env-var syntax for the colon). That's how the SQLite test harness overrides the default without editing files.

### **Step 3:** Branch AddDbContext on the flag

This is the entire behavior change. Two lines of configuration control which provider runs.

1. **Open** `src/CloudSoft.Auth.Web/Program.cs`

2. **Replace** the `AddDbContext` call with:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   var storeProvider = builder.Configuration["IdentityStore:Provider"] ?? "InMemory";
   builder.Services.AddDbContext<ApplicationDbContext>(options =>
   {
       switch (storeProvider.ToLowerInvariant())
       {
           case "sqlite":
               var connectionString = builder.Configuration.GetConnectionString("Identity")
                   ?? "Data Source=cloudsoft-auth.db";
               options.UseSqlite(connectionString);
               break;

           case "inmemory":
           default:
               options.UseInMemoryDatabase("CloudSoftAuthDb");
               break;
       }
   });
   ```

> ⚠ **Common Mistakes**
>
> - Using `AddDbContext<ApplicationDbContext>(options => …)` twice. The second call overrides the first; DI holds exactly one registration per service type.
> - Hardcoding `UseInMemoryDatabase` and `UseSqlite` without the switch — the whole point is one source of truth for the flag.

### **Step 4:** Ensure the schema exists before first use

InMemory doesn't need schema — EF creates its internal maps on first use. SQLite does: the file has to exist with the right tables. `EnsureCreated()` handles this; it creates the schema only if the DB has no tables and is a no-op on subsequent runs.

1. **In** `Program.cs`, **just before** the `TestUserSeeder.SeedAsync(...)` call, **add**:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   using (var scope = app.Services.CreateScope())
   {
       var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
       if (db.Database.IsRelational())
       {
           db.Database.EnsureCreated();
       }
   }
   ```

> ℹ **Concept Deep Dive**
>
> `EnsureCreated` is fine for demo apps, tests, and early development. For production you want **EF Core migrations** — they track schema versions and upgrade existing databases. The teaching point here is the swap mechanism, not the migration tooling.

### **Step 5:** Add a diagnostics page

A tiny endpoint that reveals the active provider makes the flag concrete. Students can flip the flag, reload, and see the change. Tests use it to verify the wiring.

1. **Create** `src/CloudSoft.Auth.Web/Controllers/DiagnosticsController.cs`:

   > `src/CloudSoft.Auth.Web/Controllers/DiagnosticsController.cs`

   ```csharp
   using CloudSoft.Auth.Web.Data;
   using Microsoft.AspNetCore.Mvc;
   using Microsoft.EntityFrameworkCore;

   namespace CloudSoft.Auth.Web.Controllers;

   public class DiagnosticsController : Controller
   {
       private readonly ApplicationDbContext _db;
       private readonly IConfiguration _config;

       public DiagnosticsController(ApplicationDbContext db, IConfiguration config)
       {
           _db = db;
           _config = config;
       }

       [HttpGet]
       public IActionResult Store()
       {
           ViewBag.ConfiguredProvider = _config["IdentityStore:Provider"] ?? "InMemory";
           ViewBag.ProviderName = _db.Database.ProviderName ?? "(unknown)";
           ViewBag.IsRelational = _db.Database.IsRelational();
           return View();
       }
   }
   ```

2. **Create** `src/CloudSoft.Auth.Web/Views/Diagnostics/Store.cshtml`:

   > `src/CloudSoft.Auth.Web/Views/Diagnostics/Store.cshtml`

   ```html
   @{
       ViewData["Title"] = "Store Diagnostics";
   }

   <h1>Identity store diagnostics</h1>

   <dl class="row">
       <dt class="col-sm-4">Configured provider</dt>
       <dd class="col-sm-8" data-testid="diag-configured-provider">@ViewBag.ConfiguredProvider</dd>

       <dt class="col-sm-4">EF Core provider in use</dt>
       <dd class="col-sm-8" data-testid="diag-provider-name">@ViewBag.ProviderName</dd>

       <dt class="col-sm-4">Is relational?</dt>
       <dd class="col-sm-8" data-testid="diag-is-relational">@ViewBag.IsRelational</dd>
   </dl>
   ```

### **Step 6:** Test Your Implementation

**InMemory (default):**

1. `dotnet run --project src/CloudSoft.Auth.Web --launch-profile http`
2. Visit `/Diagnostics/Store` — it reports `InMemory`.
3. Log in as admin, restart the app, log in again — works each time because the seeder recreates users on every boot.

**SQLite:**

1. Stop the app.
2. Set the flag and start again:

   ```bash
   IdentityStore__Provider=SQLite dotnet run --project src/CloudSoft.Auth.Web --launch-profile http
   ```

3. `/Diagnostics/Store` now reports `SQLite`, with EF Core provider `Microsoft.EntityFrameworkCore.Sqlite`.
4. A new `cloudsoft-auth.db` file appears in `src/CloudSoft.Auth.Web/`.
5. Log in as admin. Stop the app. Start it again **without wiping the DB file**. Admin still exists — the seeder's `FindByNameAsync` returned the existing row and took no action.

> ✓ **Success indicators:**
>
> - The diagnostics page reflects the flag's value
> - InMemory resets every boot; SQLite persists
> - Tests pass under both harnesses:
>   - `./run-playwright-tests.sh headless` — default InMemory
>   - `./run-playwright-tests-sqlite.sh headless` — SQLite (wipes the DB first)

## Common Issues

> **If you encounter problems:**
>
> **`InvalidOperationException: The current store provider is not registered.`:** You referenced a provider (e.g. `UseSqlServer`) without installing its NuGet package. Add the matching EF Core provider package.
>
> **SQLite DB file not appearing:** Check the working directory when you ran the app. Relative connection strings (`Data Source=cloudsoft-auth.db`) are relative to the process's CWD — typically the project directory when launched via `dotnet run`.
>
> **`EnsureCreated` throws when migrations exist:** `EnsureCreated` and migrations don't mix. If you later adopt migrations (`dotnet ef migrations add ...`), replace `EnsureCreated()` with `Migrate()`.
>
> **Test passes under InMemory but fails under SQLite:** InMemory is lenient about foreign keys and transactions. Real databases are strict. Failures here usually indicate a bug your tests caught — good news.

## Summary

You've given the app two user stores and a flag to pick between them:

- ✓ `AddDbContext` is the single point of substitution
- ✓ `IdentityStore:Provider` in configuration chooses at startup
- ✓ `EnsureCreated` bootstraps relational schemas without migrations
- ✓ A diagnostics page makes the choice visible

> **Key takeaway:** Persistence is an implementation detail behind the `DbContext`. The same is true for many infrastructure concerns (logging, messaging, caching). Design toward interfaces, register implementations behind flags, and you keep deployment-time flexibility.

## Done!

InMemory and SQLite both work. **Exercise 5.3** replaces the hardcoded `TestUserSeeder` with a config-driven admin seeder — the real-world pattern for bootstrapping the first privileged account in a fresh database.
