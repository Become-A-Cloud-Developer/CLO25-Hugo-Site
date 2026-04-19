+++
title = "3. Claims, Policies, and CSRF Protection"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Attach custom claims to the signed-in principal, gate a page with a named policy, and verify antiforgery protection on state-changing POSTs"
weight = 3
draft = false
+++

# Claims, Policies, and CSRF Protection

## Goal

Generalize what you built in Exercise 2 to any claim — not just roles — and use **named policies** to express authorization rules declaratively. Then add a small **antiforgery demo** that shows exactly what happens when a state-changing POST arrives without a CSRF token.

By the end, the Who Am I page renders the full claim list (type, value, issuer) and the user can trigger both the accept and reject paths of the antiforgery check.

> **What you'll learn:**
>
> - How to add arbitrary custom claims to a `ClaimsPrincipal`
> - How `AddAuthorization(options => options.AddPolicy(...))` registers a named policy
> - How `[Authorize(Policy = "…")]` differs from `[Authorize(Roles = "…")]`
> - What antiforgery tokens protect against and how ASP.NET Core enforces them

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed Exercise 2 — roles and `[Authorize(Roles = "…")]` working
> - ✓ The Who Am I page shows roles for each logged-in user

## Exercise Steps

### Overview

1. **Extend `DummyUsers`** with a `Claims` dictionary
2. **Emit custom claims** during sign-in in `AccountController`
3. **Register a policy** in `Program.cs`
4. **Create the Engineering page** gated by the policy
5. **Extend the Who Am I view** — full claims table and conditional link
6. **Add the CSRF demo form**
7. **Test Your Implementation**

### **Step 1:** Extend the dummy user list with claims

Roles are just claims with a well-known type. Other claims can carry anything — organization, department, age, subscription tier. Here we add a single `Department` claim per user to keep the demo focused.

1. **Replace** `src/CloudSoft.Auth.Web/Data/DummyUsers.cs`:

   > `src/CloudSoft.Auth.Web/Data/DummyUsers.cs`

   ```csharp
   namespace CloudSoft.Auth.Web.Data;

   public static class DummyUsers
   {
       public record User(
           string Username,
           string Password,
           string[] Roles,
           Dictionary<string, string> Claims);

       public static readonly List<User> All =
       [
           new("admin", "admin",
               Roles: ["Admin"],
               Claims: new() { ["Department"] = "Engineering" }),

           new("candidate", "candidate",
               Roles: ["Candidate"],
               Claims: new() { ["Department"] = "Sales" }),
       ];

       public static User? Find(string username, string password) =>
           All.FirstOrDefault(u =>
               string.Equals(u.Username, username, StringComparison.Ordinal) &&
               string.Equals(u.Password, password, StringComparison.Ordinal));
   }
   ```

### **Step 2:** Emit the custom claims at sign-in

`AccountController.Login` already loops over roles. Add a second loop for the generic claims dictionary.

1. **Open** `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

2. **After** the role `foreach`, add:

   > `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

   ```csharp
   foreach (var (type, value) in user.Claims)
   {
       claims.Add(new Claim(type, value));
   }
   ```

> ℹ **Concept Deep Dive**
>
> A claim is a tuple of `(type, value, issuer)`. The issuer defaults to `LOCAL AUTHORITY` when you build it yourself; external providers (Google, Microsoft) set the issuer to their identity URL. In Chapter 5 you'll see Google-issued claims appear in this same table with a different issuer.

### **Step 3:** Register the policy

A policy is a named set of requirements. `RequireEngineering` is satisfied when the principal carries a `Department` claim whose value is `Engineering`.

1. **Open** `src/CloudSoft.Auth.Web/Program.cs`

2. **Replace** `builder.Services.AddAuthorization();` with:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy("RequireEngineering", policy =>
           policy.RequireClaim("Department", "Engineering"));
   });
   ```

> ℹ **Concept Deep Dive**
>
> `policy.RequireClaim(type, value)` is the simplest requirement. You can also combine requirements — `.RequireRole("Admin").RequireClaim("Department", "Engineering")` means the principal must satisfy **both**. For more elaborate logic, implement `IAuthorizationRequirement` and pair it with an `AuthorizationHandler<T>`.

### **Step 4:** Create the Engineering page

The Engineering page is visible only when the policy is satisfied. `[Authorize(Policy = "...")]` looks the same as `[Authorize(Roles = "...")]` at the usage level — the difference is that `Roles` is baked in while `Policy` is arbitrary.

1. **Create** the controller:

   > `src/CloudSoft.Auth.Web/Controllers/EngineeringController.cs`

   ```csharp
   using Microsoft.AspNetCore.Authorization;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudSoft.Auth.Web.Controllers;

   [Authorize(Policy = "RequireEngineering")]
   public class EngineeringController : Controller
   {
       public IActionResult Index() => View();
   }
   ```

2. **Create** the view:

   > `src/CloudSoft.Auth.Web/Views/Engineering/Index.cshtml`

   ```html
   @{
       ViewData["Title"] = "Engineering";
   }

   <h1 data-testid="engineering-heading">Engineering</h1>
   <p class="lead">You only see this page because your identity carries <code>Department = Engineering</code>.</p>
   <p>The gate is the <code>RequireEngineering</code> policy, not a role.</p>

   <a asp-controller="WhoAmI" asp-action="Index" class="btn btn-outline-secondary">Who Am I?</a>
   ```

### **Step 5:** Show the full claims table and conditional link

The Who Am I page can now render every claim on the identity. Each row includes the issuer so you can see which came from your sign-in code versus (in Chapter 5) from Google.

1. **Open** `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

2. **Add** `@using System.Security.Claims` at the top of the file (removing the inline namespace you used in Exercise 2)

3. **Replace** the inline `ClaimTypes.Role` reference in the roles row with just `ClaimTypes.Role` (thanks to the new `@using`)

4. **Add** the claims table **after** the `</dl>`:

   > `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

   ```html
   <h2 class="mt-4">All claims</h2>
   <table class="table table-sm table-striped" data-testid="whoami-claims-table">
       <thead>
           <tr>
               <th>Type</th>
               <th>Value</th>
               <th>Issuer</th>
           </tr>
       </thead>
       <tbody>
           @if (User.Identity?.IsAuthenticated == true)
           {
               foreach (var claim in User.Claims)
               {
                   <tr>
                       <td><code>@claim.Type</code></td>
                       <td>@claim.Value</td>
                       <td>@claim.Issuer</td>
                   </tr>
               }
           }
           else
           {
               <tr>
                   <td colspan="3" class="text-muted">(no identity — log in to see claims)</td>
               </tr>
           }
       </tbody>
   </table>
   ```

5. **Replace** the admin link block with a row that also includes a conditional Engineering link:

   > `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

   ```html
   <div class="mb-4">
       @if (User.IsInRole("Admin"))
       {
           <a asp-controller="AdminOnly" asp-action="Index" class="btn btn-outline-primary me-2" data-testid="whoami-adminlink">
               Admin-only page
           </a>
       }
       @if (User.HasClaim("Department", "Engineering"))
       {
           <a asp-controller="Engineering" asp-action="Index" class="btn btn-outline-primary" data-testid="whoami-engineeringlink">
               Engineering page
           </a>
       }
   </div>
   ```

### **Step 6:** Add the antiforgery demo form

The demo is a trivial POST that echoes a message back via TempData. The action is protected with `[ValidateAntiForgeryToken]`; the form includes `@Html.AntiForgeryToken()`. Removing either side breaks the round-trip.

1. **Open** `src/CloudSoft.Auth.Web/Controllers/WhoAmIController.cs`

2. **Add** the `TestPost` action:

   > `src/CloudSoft.Auth.Web/Controllers/WhoAmIController.cs`

   ```csharp
   using Microsoft.AspNetCore.Authorization;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudSoft.Auth.Web.Controllers;

   public class WhoAmIController : Controller
   {
       public IActionResult Index() => View();

       [HttpPost]
       [Authorize]
       [ValidateAntiForgeryToken]
       public IActionResult TestPost(string message)
       {
           TempData["CsrfDemoMessage"] = $"Received: {message}";
           return RedirectToAction(nameof(Index));
       }
   }
   ```

3. **In** the Who Am I view, **add** the demo form inside the `User.Identity?.IsAuthenticated` block, **before** the existing logout form:

   > `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

   ```html
   <h2 class="mt-4">Antiforgery demo</h2>
   <p class="text-muted">
       This form is protected by <code>[ValidateAntiForgeryToken]</code>. Submitting it normally succeeds;
       a POST made without the token returns HTTP 400.
   </p>

   <form asp-controller="WhoAmI" asp-action="TestPost" method="post" data-testid="csrf-form" class="mb-3">
       @Html.AntiForgeryToken()
       <div class="input-group" style="max-width: 420px;">
           <input type="text" name="message" value="hello" class="form-control"
                  data-testid="csrf-message" />
           <button type="submit" class="btn btn-primary" data-testid="csrf-submit">Submit</button>
       </div>
   </form>

   @if (TempData["CsrfDemoMessage"] != null)
   {
       <div class="alert alert-success" data-testid="csrf-result">
           @TempData["CsrfDemoMessage"]
       </div>
   }
   ```

> ℹ **Concept Deep Dive**
>
> Antiforgery protects against cross-site request forgery: an attacker's page submits a form to your endpoint using a victim's cookie. The token is a per-session secret set as a cookie and echoed in a hidden form field; the server compares them on every POST. A third-party page can't read your antiforgery cookie (same-origin policy), so it can't produce a matching field.
>
> ⚠ **Common Mistakes**
>
> - Omitting `[ValidateAntiForgeryToken]` on MVC controller POSTs. It is **not** applied automatically.
> - Using `@Html.AntiForgeryToken()` outside the `<form>` — the hidden field must be a child of the submitted form.

### **Step 7:** Test Your Implementation

1. **Run** the app and log in as `admin`.
2. On Who Am I, confirm:
   - The `All claims` table includes `http://schemas.microsoft.com/ws/2008/06/identity/claims/role = Admin` and `Department = Engineering`.
   - An **Engineering page** button appears (and navigates successfully).
3. Submit the **Antiforgery demo** form — a green alert shows `Received: hello`.
4. Log out, log in as `candidate`:
   - Claims table now shows `Department = Sales`.
   - The Engineering button is gone. Typing `/Engineering` in the address bar redirects to `/Account/AccessDenied`.
5. **(Developer-tools test)** Open browser DevTools → Network → inspect the `TestPost` submission. The request body contains a `__RequestVerificationToken` field. Re-submit the form via curl without that token (or delete the `@Html.AntiForgeryToken()` line, rebuild, submit) and observe HTTP 400.

> ✓ **Success indicators:**
>
> - The claims table shows every claim you added plus `ClaimTypes.Name`
> - `[Authorize(Policy = "RequireEngineering")]` behaves exactly like `[Authorize(Roles = ...)]`: allow or redirect to access denied
> - The antiforgery form works when submitted normally and fails with 400 when the token is stripped

## Common Issues

> **If you encounter problems:**
>
> **Policy never satisfied:** Both the claim type and value match exactly (string equality, case-sensitive). `Department` is not the same as `department`.
>
> **`InvalidOperationException: No policy named…`:** You referenced `[Authorize(Policy = "Foo")]` without calling `AddPolicy("Foo", ...)` in `Program.cs`.
>
> **Antiforgery form always fails:** Check that `@Html.AntiForgeryToken()` is inside the form and that `[ValidateAntiForgeryToken]` is on the action method. Both are required.

## Summary

You've generalized the primitive one more step:

- ✓ Any claim, not just roles, can live on the principal
- ✓ Named policies let you express authorization rules in one place and re-use them on many controllers
- ✓ Antiforgery token protection is easy to add and catastrophic to forget

> **Key takeaway:** `[Authorize(Policy = ...)]` is the general form. `[Authorize(Roles = ...)]` is syntactic sugar over a role-claim check. Reach for policies when the rule is anything other than "is the user in this role?".

## Done!

You've completed the cookie-auth fundamentals. The final exercise in this chapter adds an **external identity provider** (Google), showing how the same `ClaimsPrincipal` abstraction works across providers.
