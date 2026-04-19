+++
title = "2. Roles and the [Authorize] Attribute"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Add Role claims to the signed-in principal, gate an admin-only page with [Authorize(Roles = \"Admin\")], and surface the current roles on the Who Am I page"
weight = 2
draft = false
+++

# Roles and the `[Authorize]` Attribute

## Goal

Extend the cookie-based authentication from Exercise 1 so users carry **roles**. Add `Admin` and `Candidate` roles to the dummy user list, emit them as claims during sign-in, and gate a new `/AdminOnly` page with `[Authorize(Roles = "Admin")]`. The Who Am I page gets a new row showing the current user's roles.

> **What you'll learn:**
>
> - How roles are represented as `ClaimTypes.Role` claims on `ClaimsPrincipal`
> - How `[Authorize(Roles = "…")]` maps to claim matching
> - How the cookie middleware handles a forbidden user (redirects to `AccessDeniedPath`)
> - How to conditionally render UI based on `User.IsInRole(...)`

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed Exercise 1 — cookie authentication and Who Am I page working
> - ✓ The project builds and the Who Am I page shows `IsAuthenticated`, `Name`, `AuthenticationType`

## Exercise Steps

### Overview

1. **Extend `DummyUsers`** with a `Roles` array
2. **Emit role claims** during sign-in in `AccountController`
3. **Create the `AdminOnly` page** guarded by `[Authorize(Roles = "Admin")]`
4. **Show roles on Who Am I** and conditionally link to the admin page
5. **Test Your Implementation**

### **Step 1:** Extend the dummy user list with roles

Roles are just strings. A user can belong to zero, one, or many. For this lab `admin` carries `Admin`, and `candidate` carries `Candidate` — two mutually-exclusive examples that make role-based tests easy to reason about.

1. **Replace** the contents of `src/CloudSoft.Auth.Web/Data/DummyUsers.cs`:

   > `src/CloudSoft.Auth.Web/Data/DummyUsers.cs`

   ```csharp
   namespace CloudSoft.Auth.Web.Data;

   public static class DummyUsers
   {
       public record User(string Username, string Password, string[] Roles);

       public static readonly List<User> All =
       [
           new("admin", "admin", Roles: ["Admin"]),
           new("candidate", "candidate", Roles: ["Candidate"]),
       ];

       public static User? Find(string username, string password) =>
           All.FirstOrDefault(u =>
               string.Equals(u.Username, username, StringComparison.Ordinal) &&
               string.Equals(u.Password, password, StringComparison.Ordinal));
   }
   ```

> ℹ **Concept Deep Dive**
>
> ASP.NET Core treats a role as a claim whose type is `ClaimTypes.Role`. There is no separate "role store" in the primitive auth stack — roles and claims are the same data shape, just a convention on the type string. `User.IsInRole("Admin")` is effectively `User.HasClaim(ClaimTypes.Role, "Admin")`.

### **Step 2:** Emit role claims during sign-in

The `ClaimsPrincipal` you build in `AccountController` needs a `Role` claim for every role the user belongs to. The cookie middleware serializes all of them, so every request that comes back with the cookie will materialize a principal whose `IsInRole` knows about them.

1. **Open** `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

2. **In** the `Login` POST action, **add** a `foreach` loop that adds a role claim per role, just after the `ClaimTypes.Name` claim:

   > `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

   ```csharp
   var claims = new List<Claim>
   {
       new(ClaimTypes.Name, user.Username),
   };

   foreach (var role in user.Roles)
   {
       claims.Add(new Claim(ClaimTypes.Role, role));
   }

   var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
   ```

> ⚠ **Common Mistakes**
>
> - Using your own string key (`"role"`, `"Roles"`) instead of `ClaimTypes.Role` — the `[Authorize(Roles = ...)]` attribute only looks at `ClaimTypes.Role`, so your roles will be invisible to it.
> - Adding the roles as a single comma-separated claim value — each role is its own claim.

### **Step 3:** Create the admin-only page

`[Authorize]` without arguments only requires the user to be authenticated. Passing `Roles = "Admin"` adds a second condition: the principal must carry a `Role` claim with value `Admin`. A logged-in Candidate hitting this page is redirected to the `AccessDeniedPath` configured in Exercise 1.

1. **Create** the controller:

   > `src/CloudSoft.Auth.Web/Controllers/AdminOnlyController.cs`

   ```csharp
   using Microsoft.AspNetCore.Authorization;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudSoft.Auth.Web.Controllers;

   [Authorize(Roles = "Admin")]
   public class AdminOnlyController : Controller
   {
       public IActionResult Index() => View();
   }
   ```

2. **Create** the view:

   > `src/CloudSoft.Auth.Web/Views/AdminOnly/Index.cshtml`

   ```html
   @{
       ViewData["Title"] = "Admin only";
   }

   <h1 data-testid="adminonly-heading">Admin only</h1>
   <p class="lead">You only see this page because your identity carries the <code>Admin</code> role.</p>

   <a asp-controller="WhoAmI" asp-action="Index" class="btn btn-outline-secondary">Who Am I?</a>
   ```

> ℹ **Concept Deep Dive**
>
> `[Authorize(Roles = "Admin,Moderator")]` lets you accept any of several roles (the principal must carry at least one). Stacking two attributes — `[Authorize(Roles = "Admin")] [Authorize(Roles = "Moderator")]` — means the principal must carry **both** roles. Slightly different semantics; the first is usually what you want.

### **Step 4:** Show roles on the Who Am I page

The Who Am I page should reveal what the principal currently carries. Add a roles row and — because there is now something gated — a conditional link to the admin page.

1. **Open** `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

2. **Add** a `Roles` row inside the definition list, and **add** the admin link block just after the `</dl>`:

   > `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

   ```html
   <dt class="col-sm-4">Roles</dt>
   <dd class="col-sm-8" data-testid="whoami-roles">
       @{
           var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                           .Select(c => c.Value).ToArray();
       }
       @(roles.Length == 0 ? "(none)" : string.Join(", ", roles))
   </dd>
   ```

   After the `</dl>` (and before the `@if (User.Identity?.IsAuthenticated == true)` block):

   ```html
   @if (User.IsInRole("Admin"))
   {
       <a asp-controller="AdminOnly" asp-action="Index" class="btn btn-outline-primary me-2" data-testid="whoami-adminlink">
           Admin-only page
       </a>
   }
   ```

### **Step 5:** Test Your Implementation

1. **Run** the app:

   ```bash
   dotnet run --project src/CloudSoft.Auth.Web --launch-profile http
   ```

2. **Log in as admin** (`admin` / `admin`) and go to **Who Am I?**:
   - The `Roles` row shows `Admin`
   - An **Admin-only page** button is visible
   - Clicking it loads `/AdminOnly` successfully

3. **Log out**, **log in as candidate** (`candidate` / `candidate`):
   - The `Roles` row shows `Candidate`
   - The **Admin-only page** button is **not** visible on Who Am I
   - Typing `/AdminOnly` in the address bar redirects you to `/Account/AccessDenied`

4. **Log out** entirely and type `/AdminOnly` again:
   - The cookie middleware redirects you to `/Account/Login` with `?returnUrl=/AdminOnly`
   - After signing in as admin you land on `/AdminOnly`

> ✓ **Success indicators:**
>
> - Role claims flow from `DummyUsers` through the cookie and back into `HttpContext.User`
> - `[Authorize(Roles = "Admin")]` lets admin through and bounces everyone else
> - The admin link on Who Am I is purely visual — even hidden it cannot be bypassed because the server re-checks on the `/AdminOnly` request

## Common Issues

> **If you encounter problems:**
>
> **Admin login still can't access `/AdminOnly`:** The role claim is probably missing. Log in and view Who Am I — the roles row should list `Admin`. If it says `(none)`, the `foreach` in `AccountController` isn't running. A common cause is rebuilding `DummyUsers.User` without the `Roles` parameter.
>
> **Candidate lands on a blank page instead of AccessDenied:** Check that `AccessDeniedPath = "/Account/AccessDenied"` is still configured on `AddCookie`.
>
> **The admin link stays visible for candidates:** `User.IsInRole("Admin")` uses exact string matching. If you typed `admin` (lowercase) in `DummyUsers`, the check won't find it.

## Summary

You've added the second primitive on top of Exercise 1's cookie authentication:

- ✓ Role claims added to the signed-in `ClaimsPrincipal`
- ✓ `[Authorize(Roles = "Admin")]` gating a controller
- ✓ Server-side 403 via `AccessDeniedPath`, not a client-side hide-and-hope
- ✓ Conditional UI using `User.IsInRole(...)`

> **Key takeaway:** Roles are ordinary claims with a conventional type (`ClaimTypes.Role`). Understanding that unifies the mental model — the next exercise generalizes to **any** claim and to **policies** that combine them.

## Done!

Admin and Candidate now see different pages. Next: arbitrary claims and policy-based authorization, generalizing what you've built so far.
