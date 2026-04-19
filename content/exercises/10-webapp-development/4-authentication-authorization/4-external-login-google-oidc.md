+++
title = "4. External Login with Google OIDC"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Wire up Google as an external identity provider so users can sign in with a Google account. The cookie carries Google-issued claims; no local user store is required."
weight = 4
draft = true
+++

# External Login with Google OIDC

## Goal

Add Google as an **external identity provider**. After wiring it up, users can click **Sign in with Google**, consent on Google's screen, and come back signed in to your app — with Google-issued claims on their `ClaimsPrincipal`. The cookie scheme you built in Exercise 1 is still the one that holds the session; Google only handles the authentication hand-off.

This is the first time the Who Am I page will show a claim whose **issuer** is not your application.

> **What you'll learn:**
>
> - How OAuth 2.0 / OIDC actually flows through ASP.NET Core middleware
> - How `AddGoogle()` registers an additional authentication handler alongside the cookie
> - How `Challenge()` initiates the provider round-trip
> - Why external login without a user store is a useful teaching tool but not a production pattern

## Prerequisites

> **Before starting, ensure you have:**
>
> - ✓ Completed Exercises 1–3
> - ✓ A Google account (for creating an OAuth 2.0 client)

## Register a Google OAuth client

Before touching code, you need a Google OAuth 2.0 client ID and secret. This is the only manual external step; everything else is local.

1. Open <https://console.cloud.google.com/apis/credentials> (sign in with your Google account)
2. **Create project** if you don't already have one
3. **APIs & Services → OAuth consent screen** — configure as an *External* test app with your email as a tester
4. **Credentials → Create Credentials → OAuth client ID** — choose *Web application*
5. **Authorized redirect URIs**: add `http://localhost:5017/signin-google`
6. **Create**. Copy the **Client ID** and **Client secret**.

> ⚠ **Common Mistakes**
>
> - The redirect URI must match exactly. `http` vs `https`, port mismatch, missing path — any of these will cause `redirect_uri_mismatch` errors.
> - For production you need an additional redirect URI (the public domain). Dev and prod are separate Google OAuth clients in real deployments.

## Store the secrets locally

Never commit OAuth secrets to source. The standard dev pattern is ASP.NET Core's **user-secrets** store — a JSON file per project stored outside the repo.

```bash
cd src/CloudSoft.Auth.Web
dotnet user-secrets init                                          # once per project
dotnet user-secrets set "Authentication:Google:ClientId"     "<your-client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>"
```

In production (Azure), the same keys come from environment variables or Key Vault. The configuration layer abstracts over both.

## Exercise Steps

### Overview

1. **Install** the Google authentication package
2. **Register** Google conditionally in `Program.cs`
3. **Add** the `ExternalLogin` POST action to `AccountController`
4. **Render** external-login buttons on the Login view
5. **Test Your Implementation**

### **Step 1:** Install the Google auth package

```bash
dotnet add src/CloudSoft.Auth.Web package Microsoft.AspNetCore.Authentication.Google
```

### **Step 2:** Conditionally register Google

Only register the handler when both configuration values are present. This keeps the reference app and tests working without any Google setup — the button simply disappears when the provider isn't wired.

1. **Open** `src/CloudSoft.Auth.Web/Program.cs`

2. **Replace** the `AddAuthentication` / `AddCookie` block with:

   > `src/CloudSoft.Auth.Web/Program.cs`

   ```csharp
   var authBuilder = builder.Services
       .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(options =>
       {
           options.LoginPath = "/Account/Login";
           options.AccessDeniedPath = "/Account/AccessDenied";
       });

   var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
   var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
   if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
   {
       authBuilder.AddGoogle(options =>
       {
           options.ClientId = googleClientId;
           options.ClientSecret = googleClientSecret;
       });
   }
   ```

> ℹ **Concept Deep Dive**
>
> `AddGoogle()` registers a new authentication scheme named `Google` (the default). Its `SignInScheme` defaults to the cookie scheme, so after a successful Google round-trip the user ends up with exactly the same kind of auth cookie a local sign-in would produce — just populated with different claims.

### **Step 3:** Add the ExternalLogin action

The action's job is to start the round-trip. `Challenge(provider)` tells ASP.NET Core to invoke that scheme's challenge handler, which for Google is a 302 to Google's authorize endpoint.

1. **Open** `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

2. **Add** the action (alongside `Login` and `Logout`):

   > `src/CloudSoft.Auth.Web/Controllers/AccountController.cs`

   ```csharp
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
   ```

> ⚠ **Common Mistakes**
>
> - Passing an absolute URL in `returnUrl` without `Url.IsLocalUrl(...)` is an open-redirect bug.
> - Forgetting `[AllowAnonymous]` — the user isn't signed in yet when they click the button, so requiring authentication would block the flow.

### **Step 4:** Show external-login buttons on the Login view

Querying `IAuthenticationSchemeProvider` at render time lets the view discover which providers are registered without hardcoding knowledge of Google. Add a provider tomorrow and the button appears automatically.

1. **Open** `src/CloudSoft.Auth.Web/Views/Account/Login.cshtml`

2. **Add** at the top of the file:

   > `src/CloudSoft.Auth.Web/Views/Account/Login.cshtml`

   ```csharp
   @using Microsoft.AspNetCore.Authentication
   @inject IAuthenticationSchemeProvider SchemeProvider
   @{
       ViewData["Title"] = "Log in";
       var externalSchemes = (await SchemeProvider.GetAllSchemesAsync())
           .Where(s => !string.IsNullOrEmpty(s.DisplayName))
           .ToArray();
   }
   ```

3. **Append** below the existing dummy-credentials paragraph:

   > `src/CloudSoft.Auth.Web/Views/Account/Login.cshtml`

   ```html
   @if (externalSchemes.Any())
   {
       <hr />
       <h2>Or sign in with</h2>
       <form method="post" asp-controller="Account" asp-action="ExternalLogin" data-testid="external-login-form">
           @Html.AntiForgeryToken()
           <input type="hidden" name="returnUrl" value="@ViewData["ReturnUrl"]" />
           @foreach (var scheme in externalSchemes)
           {
               <button type="submit" name="provider" value="@scheme.Name"
                       class="btn btn-outline-primary me-2"
                       data-testid="external-login-@scheme.Name.ToLowerInvariant()">
                   @scheme.DisplayName
               </button>
           }
       </form>
   }
   ```

> ℹ **Concept Deep Dive**
>
> ASP.NET Core only exposes schemes with a non-empty `DisplayName`. Google's handler sets `DisplayName = "Google"` by default. This is the canonical way to enumerate "visible" providers — the cookie scheme has no display name and so is correctly skipped.

### **Step 5:** Test Your Implementation

Without the Google configuration set, the Login page looks identical to Exercise 3. After `dotnet user-secrets set` with real values, restart the app and:

1. Go to `/Account/Login`. A **Sign in with Google** button appears below the classic login form.
2. Click it. You end up on Google's consent screen.
3. Approve. Google redirects back to `/signin-google`. The cookie middleware completes the sign-in and lands you on `/WhoAmI`.
4. **Who Am I?** now shows:
   - `IsAuthenticated: True`
   - `Name`: your Google display name or email
   - Claims table includes entries with `Issuer = https://accounts.google.com`, including your Google profile picture, email, and Google subject ID

Log out — the classic admin/candidate flow still works in the same session.

> ✓ **Success indicators:**
>
> - With no Google config, the site behaves exactly as at the end of Exercise 3
> - With Google config, the button appears and the round-trip succeeds
> - Google-issued claims are visible on Who Am I alongside (or instead of) your hand-built claims

## Common Issues

> **If you encounter problems:**
>
> **`redirect_uri_mismatch`:** Google is redirecting to a URI you didn't authorize. Compare exactly (`http://localhost:5017/signin-google`).
>
> **`invalid_client`:** Client ID and secret don't match. Re-copy from the Google Cloud console.
>
> **Button doesn't appear:** Confirm `dotnet user-secrets list` shows both keys; restart the app (user-secrets are loaded at startup).
>
> **Works in dev, 500 in production:** Production needs a separate OAuth client with your public HTTPS redirect URI. It also needs the cookie data protection keys persisted — otherwise the auth cookie changes on every restart.

## Summary

You've added the final piece of cookie-based authentication:

- ✓ A real OAuth 2.0 / OIDC provider signs the user in
- ✓ Google-issued claims flow into the same `ClaimsPrincipal` as your hand-built ones
- ✓ The cookie is still the session boundary — external providers just change where claims originate

> **Key takeaway:** External providers don't replace your authentication — they **contribute** claims to it. Production apps pair external login with a local user store (ASP.NET Core Identity, next chapter) to persist which external users exist and what roles they have.

## Done!

Chapter 4 is complete. You now understand the entire cookie-authentication stack: the cookie itself, the `ClaimsPrincipal` it carries, roles and custom claims, named policies, antiforgery protection, and external providers.

**Chapter 5** adds persistence: ASP.NET Core Identity, a feature-flagged InMemory/SQLite user store, startup admin seeding, and real registration with role promotion.
