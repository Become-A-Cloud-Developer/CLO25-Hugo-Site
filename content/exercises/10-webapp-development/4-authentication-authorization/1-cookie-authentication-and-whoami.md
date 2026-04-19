+++
title = "1. Cookie Authentication and the Who Am I Page"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Wire up cookie authentication against a hardcoded user list and expose a Who Am I page that reveals the current authentication state"
weight = 1
draft = false
+++

# Cookie Authentication and the Who Am I Page

## Goal

Add cookie-based authentication to a fresh ASP.NET Core MVC app using hardcoded dummy credentials and build a **Who Am I?** page that shows the current identity state (authenticated or not, the signed-in user's name, and which authentication scheme issued the identity). This page becomes the laboratory surface for the rest of the chapter.

> **What you'll learn:**
>
> - How the authentication and authorization middleware fit into the request pipeline
> - How to hand-build a `ClaimsPrincipal` and sign it into a cookie with `HttpContext.SignInAsync`
> - How `User.Identity.Name`, `IsAuthenticated`, and `AuthenticationType` surface that cookie back to your code
> - Why the order of `UseAuthentication` and `UseAuthorization` matters

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ .NET 10 SDK installed (`dotnet --version` reports a `10.*` version)
> - ✓ A Git-tracked working directory for the exercise (a fresh folder is fine)
> - ✓ Basic familiarity with controllers, views, and `Program.cs`
>
> You can also start from the pre-made starter at `reference/CloudSoft-Auth/` in the course repository (the output of Step 1 below, already committed). Copy it to your work directory and skip ahead to Step 2.

## Exercise Steps

### Overview

1. **Scaffold** a new ASP.NET Core MVC project
2. **Register cookie authentication** in `Program.cs`
3. **Create a hardcoded user list** in `Data/DummyUsers.cs`
4. **Build the `AccountController`** with Login and Logout actions
5. **Build the `WhoAmIController`** and view that renders the identity state
6. **Add the footer link** to the Who Am I page
7. **Test Your Implementation**

### **Step 1:** Scaffold a new ASP.NET Core MVC project

Start from the default MVC template. The scaffold gives you a home controller, a shared layout, and Bootstrap pre-wired — everything you'll extend over this chapter without distractions from extra boilerplate.

1. **Create** the project folder and scaffold the app:

   ```bash
   mkdir -p src
   dotnet new mvc -o src/CloudSoft.Auth.Web --framework net10.0
   ```

2. **Verify** it builds and runs:

   ```bash
   dotnet build src/CloudSoft.Auth.Web
   dotnet run --project src/CloudSoft.Auth.Web --launch-profile http
   ```

   Opening `http://localhost:5017` should show the default MVC welcome page. Stop the server with `Ctrl+C`.

3. **Remove** the HTTPS redirection from the scaffold. HTTPS is the right default for production, but the development certificate makes Playwright setup fiddly and we're going to hit `http://localhost:5017` throughout the chapter. Open `src/CloudSoft.Auth.Web/Program.cs` and **remove** the `UseHttpsRedirection()` call and the `UseHsts` block:

   > `src/CloudSoft.Auth.Web/Program.cs` (changes shown with `-` / `+`)

   ```diff
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
   -    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
   -    app.UseHsts();
    }

   -app.UseHttpsRedirection();
    app.UseRouting();
   ```

> ℹ **Concept Deep Dive**
>
> In production you absolutely want HTTPS and HSTS. In this lab, the app runs only on your development machine and we're going to restart it often, so the HTTP-only setup is a deliberate simplification. When you apply these patterns to your CloudSoft-Recruitment project, leave the HTTPS block in.
>
> ✓ **Quick check:** The project builds and `http://localhost:5017` loads the default MVC home page.

### **Step 2:** Register cookie authentication

Cookie authentication is the simplest auth scheme ASP.NET Core ships with. On sign-in, the framework serializes a `ClaimsPrincipal` into an encrypted cookie; on every subsequent request the middleware deserializes that cookie back into `HttpContext.User`. To use it, you register the scheme with dependency injection and add two middleware to the request pipeline.

1. **Open** `src/CloudSoft.Auth.Web/Program.cs`

2. **Replace** the file with the following:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   using Microsoft.AspNetCore.Authentication.Cookies;

   var builder = WebApplication.CreateBuilder(args);

   // Add services to the container.
   builder.Services.AddControllersWithViews();

   builder.Services
       .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.LoginPath = "/Account/Login";
           options.AccessDeniedPath = "/Account/AccessDenied";
       });

   builder.Services.AddAuthorization();

   var app = builder.Build();

   if (!app.Environment.IsDevelopment())
   {
       app.UseExceptionHandler("/Home/Error");
   }

   app.UseRouting();

   app.UseAuthentication();
   app.UseAuthorization();

   app.MapStaticAssets();

   app.MapControllerRoute(
       name: "default",
       pattern: "{controller=Home}/{action=Index}/{id?}")
       .WithStaticAssets();

   app.Run();
   ```

> ℹ **Concept Deep Dive**
>
> `AddAuthentication(scheme)` tells ASP.NET Core which authentication scheme to use by default when a request arrives without one specified. `AddCookie` registers the handler for that scheme. `LoginPath` is where the middleware redirects anonymous users who hit a protected action; `AccessDeniedPath` is where it redirects signed-in users who lack the required role or claim.
>
> ⚠ **Common Mistakes**
>
> - `UseAuthentication()` **must** come before `UseAuthorization()`. Reversed, the authorization middleware sees an anonymous request every time because no `ClaimsPrincipal` has been built yet.
> - Both middleware must come **after** `UseRouting()` so the matched endpoint is known.
>
> ✓ **Quick check:** `dotnet build` succeeds with no errors.

### **Step 3:** Create the hardcoded user list

Real applications store users in a database. For this chapter we want to keep the focus on the cookie mechanics, so we use a static in-memory list instead. The data structure deliberately carries only a username and password for now — we'll extend it with roles and claims in later exercises.

1. **Create** a new folder: `src/CloudSoft.Auth.Web/Data/`

2. **Add** a new file:

   > `src/CloudSoft.Auth.Web/Data/DummyUsers.cs`

   ```csharp
   namespace CloudSoft.Auth.Web.Data;

   public static class DummyUsers
   {
       public record User(string Username, string Password);

       public static readonly List<User> All =
       [
           new("admin", "admin"),
           new("candidate", "candidate"),
       ];

       public static User? Find(string username, string password) =>
           All.FirstOrDefault(u =>
               string.Equals(u.Username, username, StringComparison.Ordinal) &&
               string.Equals(u.Password, password, StringComparison.Ordinal));
   }
   ```

> ⚠ **Common Mistakes**
>
> - Storing passwords in source code is **only** acceptable for a lab. Never do this in production — ASP.NET Core Identity (Chapter 5) handles proper password hashing and persistence for you.

### **Step 4:** Build the AccountController

The `AccountController` handles two flows: showing the login form and processing submitted credentials. On a successful POST it builds a `ClaimsPrincipal` containing one claim — the user's name — and hands it to `HttpContext.SignInAsync`, which encrypts and sets the auth cookie on the response.

1. **Create** a new file:

   > `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

   ```csharp
   using System.Security.Claims;
   using CloudSoft.Auth.Web.Data;
   using Microsoft.AspNetCore.Authentication;
   using Microsoft.AspNetCore.Authentication.Cookies;
   using Microsoft.AspNetCore.Mvc;

   namespace CloudSoft.Auth.Web.Controllers;

   public class AccountController : Controller
   {
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
           var user = DummyUsers.Find(username, password);
           if (user is null)
           {
               ModelState.AddModelError(string.Empty, "Invalid username or password.");
               ViewData["ReturnUrl"] = returnUrl;
               return View();
           }

           var claims = new List<Claim>
           {
               new(ClaimTypes.Name, user.Username),
           };

           var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
           var principal = new ClaimsPrincipal(identity);

           await HttpContext.SignInAsync(
               CookieAuthenticationDefaults.AuthenticationScheme,
               principal);

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
           await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
           return RedirectToAction("Index", "Home");
       }

       [HttpGet]
       public IActionResult AccessDenied() => View();
   }
   ```

2. **Create** the Login view:

   > `src/CloudSoft.Auth.Web/Views/Account/Login.cshtml`

   ```html
   @{
       ViewData["Title"] = "Log in";
   }

   <h1>Log in</h1>

   @if (!ViewData.ModelState.IsValid)
   {
       <div class="alert alert-danger" data-testid="login-error">
           @Html.ValidationSummary()
       </div>
   }

   <form method="post" data-testid="login-form" style="max-width: 420px;">
       @Html.AntiForgeryToken()
       <input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />

       <div class="mb-3">
           <label for="username" class="form-label">Username</label>
           <input type="text" id="username" name="username" class="form-control"
                  data-testid="login-username" autocomplete="username" />
       </div>

       <div class="mb-3">
           <label for="password" class="form-label">Password</label>
           <input type="password" id="password" name="password" class="form-control"
                  data-testid="login-password" autocomplete="current-password" />
       </div>

       <button type="submit" class="btn btn-primary" data-testid="login-submit">Log in</button>
   </form>

   <p class="mt-4 text-muted">
       Dummy credentials for this exercise:
       <code>admin / admin</code> or <code>candidate / candidate</code>.
   </p>
   ```

3. **Create** the AccessDenied view:

   > `src/CloudSoft.Auth.Web/Views/Account/AccessDenied.cshtml`

   ```html
   @{
       ViewData["Title"] = "Access denied";
   }

   <h1 data-testid="access-denied-heading">Access denied</h1>
   <p class="lead">You are signed in but your account is not permitted to view that page.</p>

   <a asp-controller="WhoAmI" asp-action="Index" class="btn btn-outline-secondary">Who am I?</a>
   ```

> ℹ **Concept Deep Dive**
>
> `ClaimsPrincipal` is ASP.NET Core's universal user abstraction. It wraps one or more `ClaimsIdentity` objects, each a bag of `Claim` tuples: (type, value, issuer). After sign-in the middleware deserializes the cookie back into this shape on every request, and your controllers read it via `HttpContext.User`.
>
> ⚠ **Common Mistakes**
>
> - Omitting `[ValidateAntiForgeryToken]` on state-changing POSTs opens the app to CSRF. The token pairs with `@Html.AntiForgeryToken()` in the form.
> - Returning an absolute URL from `returnUrl` without validating `Url.IsLocalUrl` is an open-redirect vulnerability.
>
> ✓ **Quick check:** The project builds. The login view renders at `/Account/Login`.

### **Step 5:** Build the WhoAmI controller and view

The Who Am I page is the single window into the current authentication state. It reads `HttpContext.User` — the `ClaimsPrincipal` the cookie middleware materialized — and renders what it finds. Later exercises will extend this view; today it shows three fields and a login/logout control.

1. **Create** the controller:

   > `src/CloudSoft.Auth.Web/Controllers/WhoAmIController.cs`

   ```csharp
   using Microsoft.AspNetCore.Mvc;

   namespace CloudSoft.Auth.Web.Controllers;

   public class WhoAmIController : Controller
   {
       public IActionResult Index() => View();
   }
   ```

2. **Create** the view:

   > `src/CloudSoft.Auth.Web/Views/WhoAmI/Index.cshtml`

   ```html
   @{
       ViewData["Title"] = "Who Am I?";
   }

   <h1>Who Am I?</h1>
   <p class="lead">What the server currently knows about you.</p>

   <dl class="row">
       <dt class="col-sm-4">Is authenticated?</dt>
       <dd class="col-sm-8" data-testid="whoami-authenticated">
           @(User.Identity?.IsAuthenticated ?? false)
       </dd>

       <dt class="col-sm-4">Name</dt>
       <dd class="col-sm-8" data-testid="whoami-name">
           @(User.Identity?.Name ?? "(anonymous)")
       </dd>

       <dt class="col-sm-4">Authentication type</dt>
       <dd class="col-sm-8" data-testid="whoami-authtype">
           @(User.Identity?.AuthenticationType ?? "(none)")
       </dd>
   </dl>

   @if (User.Identity?.IsAuthenticated == true)
   {
       <form asp-controller="Account" asp-action="Logout" method="post" data-testid="logout-form">
           @Html.AntiForgeryToken()
           <button type="submit" class="btn btn-outline-secondary" data-testid="logout-submit">
               Log out
           </button>
       </form>
   }
   else
   {
       <a asp-controller="Account" asp-action="Login" class="btn btn-primary" data-testid="whoami-loginlink">Log in</a>
   }
   ```

> ℹ **Concept Deep Dive**
>
> `User.Identity` returns an `IIdentity` that is null-safe only if the request has no authentication data at all. With cookie authentication registered, the middleware always gives you an identity — possibly unauthenticated. `IsAuthenticated` is the check that tells you the cookie was present and valid.

### **Step 6:** Link the Who Am I page from the footer

You want the Who Am I page accessible from everywhere in the app. The simplest place is the shared footer.

1. **Open** `src/CloudSoft.Auth.Web/Views/Shared/_Layout.cshtml`

2. **Replace** the footer block with:

   > `src/CloudSoft.Auth.Web/Views/Shared/_Layout.cshtml`

   ```html
   <footer class="border-top footer text-muted">
       <div class="container">
           &copy; 2026 - CloudSoft.Auth.Web -
           <a asp-area="" asp-controller="Home" asp-action="Privacy">Privacy</a> -
           <a asp-area="" asp-controller="WhoAmI" asp-action="Index" data-testid="footer-whoami">Who Am I?</a>
       </div>
   </footer>
   ```

### **Step 7:** Test Your Implementation

1. **Run the app:**

   ```bash
   dotnet run --project src/CloudSoft.Auth.Web --launch-profile http
   ```

2. **Browse to** `http://localhost:5017` and click **Who Am I?** in the footer.

3. **Verify anonymous state:**
   - `Is authenticated?` shows `False`
   - `Name` shows `(anonymous)`
   - `Authentication type` shows `(none)`
   - A **Log in** button is present

4. **Verify the login flow:**
   - Click **Log in**, submit `admin` / `admin`
   - You land back on **Who Am I?**
   - `Is authenticated?` now shows `True`
   - `Name` shows `admin`
   - `Authentication type` shows `Cookies`

5. **Verify the logout flow:**
   - Click **Log out**
   - You return to the home page
   - Revisit **Who Am I?** and confirm it reports anonymous again

6. **Verify error handling:**
   - Go to `/Account/Login` and submit bad credentials (for example `admin` / `wrong`)
   - An error message appears and you stay on the login page

> ✓ **Success indicators:**
>
> - The Who Am I page correctly reports both authenticated and anonymous states
> - Login sets a cookie that survives page navigation
> - Logout clears the cookie and the anonymous state returns
> - Bad credentials show a validation error

## Common Issues

> **If you encounter problems:**
>
> **"No authentication scheme was specified" at runtime:** The middleware is registered but `AddAuthentication` wasn't called, or the scheme name doesn't match. Confirm `CookieAuthenticationDefaults.AuthenticationScheme` is used consistently.
>
> **Login redirects you straight back to the login page:** Either `UseAuthentication` is missing from the pipeline, or the order is reversed — authentication must come before authorization.
>
> **The logout button does nothing:** `Logout` is a POST, so a GET request (for example typing `/Account/Logout` in the address bar) returns 405. The `<form method="post">` wrapper is how the view submits it.
>
> **Antiforgery token errors on login:** `@Html.AntiForgeryToken()` must be inside the form; `[ValidateAntiForgeryToken]` must be on the `HttpPost` action. Both halves are required.

## Summary

You've wired up cookie authentication against a hardcoded user list and built the Who Am I page that shows its effect in real time. You've seen the three primitives the rest of the chapter builds on:

- ✓ `ClaimsPrincipal` — the universal user abstraction
- ✓ `SignInAsync` / `SignOutAsync` — the APIs that set and clear the cookie
- ✓ `UseAuthentication` / `UseAuthorization` middleware — the pipeline that turns the cookie back into `HttpContext.User`

> **Key takeaway:** Authentication in ASP.NET Core is just a `ClaimsPrincipal` wrapped in a cookie. Every method you'll learn in later exercises — roles, claims, policies, external providers — is a different way of building or interpreting that principal.

## Done!

The Who Am I page works. Next exercise: add **roles** to the principal and use `[Authorize(Roles = "Admin")]` to gate an admin-only page.
