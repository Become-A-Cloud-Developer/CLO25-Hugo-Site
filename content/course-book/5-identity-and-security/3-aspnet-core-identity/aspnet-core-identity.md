+++
title = "ASP.NET Core Identity"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/3-aspnet-core-identity.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/3-aspnet-core-identity-swe.html)

---

A web application that lets users register, sign in, change passwords, and recover forgotten accounts has to solve a long list of small but unforgiving problems: storing passwords safely, generating reset tokens that cannot be guessed, locking accounts after repeated failures, and stitching all of this onto the cookie-authentication transport so the browser stays signed in. Writing this code from scratch is the wrong default — every shortcut becomes a security incident, and every team that tries it ends up reinventing the same data structures. ASP.NET Core ships a framework component that already solves these problems and slots into a [cookie-authenticated](/course-book/5-identity-and-security/2-cookie-authentication/) application without changing the request pipeline.

## What ASP.NET Core Identity provides

**ASP.NET Core Identity** is the framework's user-management system: it stores user records (with hashed passwords), provides `UserManager<TUser>` and `SignInManager<TUser>` services, and integrates with cookie authentication to handle registration, sign-in, password reset, and lockout. It is a library, not a product — it ships as NuGet packages (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` and friends) that an application opts into during service registration in `Program.cs`.

The component sits underneath the sign-in flow rather than alongside it. Cookie [authentication](/course-book/5-identity-and-security/1-authentication-vs-authorization/) answers the question *how does the server recognise this browser as already signed in?* — encrypted ticket, `Set-Cookie` header, middleware decoding the ticket on every request. Identity answers a different question: *given a username and password the user just typed, is there a matching record, and what does that record contain?* The two systems compose: `SignInManager` validates the credential against the user database, then asks the cookie-authentication handler to issue the ticket. Without Identity, an application can still use cookies; without cookies, Identity has no way to keep the user signed in. Identity provides the user database; cookie authentication provides the session transport.

### The IdentityUser data model

Identity defines a base entity called `IdentityUser` that represents one row in the user table. Out of the box it carries fields that every credential-based system eventually needs: a primary key (`Id`, a GUID by default), the username, a normalised version of the username for case-insensitive lookup, the email address (also normalised), the password hash, a security stamp that changes whenever the user's credentials are rotated, a phone number, two-factor-authentication state, lockout state, and an access-failure counter.

Most applications need to store more than this — a display name, a profile picture URL, a tenant ID, a created-at timestamp. The framework expects this and exposes a clean extension point: derive a class from `IdentityUser`, add the extra columns, and tell Identity to use the derived type. Figure 1 shows a minimal extension.

Figure 1: Extending `IdentityUser` with application fields

```csharp
public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

The derived type flows through the rest of the framework as a generic parameter — `UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`, `IdentityDbContext<ApplicationUser>` — so the application's controllers see the full type, with the application-specific properties available, while the framework continues to operate on the base contract.

## User stores: where the records live

A **user store** is the persistence backend ASP.NET Core Identity uses to read and write user records; common implementations include in-memory (development only), Entity Framework Core against SQL, and custom implementations targeting other databases. The user store is the [repository pattern](/course-book/4-data-access/2-orm-and-repository-pattern/) applied to identity data: a thin abstraction (`IUserStore<TUser>`, `IUserPasswordStore<TUser>`, `IUserRoleStore<TUser>` and a handful of related interfaces) that the manager services call into without knowing how the data is actually stored.

This indirection matters because credential storage requirements vary. A unit-test suite wants an in-memory store that disappears between runs. A development environment wants a local SQLite file. A production deployment wants SQL Server, PostgreSQL, or a managed database service. A multi-tenant SaaS application might have a custom store that shards users across tenant-specific databases. The controller code does not change between these — `userManager.FindByNameAsync(name)` returns the user record regardless of where it is read from.

### EF Core: the default store

The most common configuration uses Entity Framework Core against a relational database. The application defines a `DbContext` that derives from `IdentityDbContext<TUser>`, and the EF-backed user store reads and writes through that context. Identity's migrations create the tables — `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims` — with the columns required by the base entities and any extras introduced by derived types.

Service registration combines the two halves with one chained call:

Figure 2: Registering Identity with an EF Core store

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Password.RequiredLength = 12;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();
```

`AddDefaultIdentity` registers `UserManager`, `SignInManager`, the cookie authentication handler used for the sign-in scheme, the password hasher, the validators, and several supporting services — using the framework's [dependency-injection container](/course-book/3-application-development/6-dependency-injection/). `AddEntityFrameworkStores` plugs in the EF-backed user store implementations. Configuration callbacks tune password rules, lockout thresholds, and sign-in requirements; everything else uses sensible defaults.

### In-memory and custom stores

The EF Core InMemory provider is appropriate only for tests and short-lived demos: data lives in process memory and is lost when the application restarts. The Identity exercises start with this provider so students can register users and sign them in without setting up a database, then [feature-flag a switch to SQLite](/exercises/10-webapp-development/5-identity-and-user-stores/) so the same code persists across restarts.

When neither EF nor in-memory fits — for example, when users live in an existing legacy database with a different schema, or in an external directory service — the application implements `IUserStore<TUser>` and the related interfaces directly. The manager services do not change; they call the same interface methods, and the custom implementation translates each call into the appropriate query against the target store. This is rarely the right starting point, but it is the escape hatch when the default store is wrong.

## UserManager and SignInManager

Two services do most of the work, and the split between them is deliberate.

`UserManager<TUser>` is the user-database API. It exposes operations on the user records themselves — create a user, find a user by name or email, change a password, generate a password-reset token, validate that a token is still valid, add a user to a [role](/course-book/5-identity-and-security/4-roles-claims-and-policies/), check email-confirmation status, count failed sign-in attempts. Every method is asynchronous and returns an `IdentityResult` carrying either success or a list of errors. `UserManager` does not know about cookies, sessions, or HTTP context; it is a pure data-and-rules service.

`SignInManager<TUser>` sits one layer above and orchestrates the sign-in flow. Its `PasswordSignInAsync(username, password, isPersistent, lockoutOnFailure)` method calls `UserManager.FindByNameAsync` to load the user, calls the password hasher to verify the supplied password against the stored hash, increments the failed-attempt counter on mismatch (and locks the account when the threshold is reached), and on success calls into the cookie-authentication handler to issue the encrypted ticket. The manager also handles sign-out, two-factor sign-in, external-login sign-in (the OAuth/OIDC entry point covered later in this Part), and re-validating the principal when the security stamp changes.

The reason these services are registered with dependency injection rather than exposed as static classes is that they depend on the user store, the password hasher, the option configuration, and the HTTP context — all of which are themselves DI-managed and request-scoped. A static `UserManager.FindByName` would have nowhere to find its store.

### Password hashing

**Password hashing** transforms a plaintext password into a fixed-length value through a slow, salted one-way function (PBKDF2, bcrypt, Argon2), making it computationally infeasible to recover the original password from the hash; the user store keeps only the hash. Identity's default hasher uses PBKDF2 with HMAC-SHA-512, a per-user random salt, and a configurable iteration count (310,000 in the current default). The salt and iteration count are stored alongside the hash, so the same hasher can verify passwords created under previous parameter sets and produce a fresh hash with current parameters when the user signs in successfully.

The slowness is the point. A PBKDF2 verification takes a fraction of a second on the server — invisible to the user — but turns an offline attack against a stolen password column into a multi-year compute job. Combined with per-user salts, the hasher prevents rainbow-table attacks: each user's hash is unique even when two users picked the same password.

The application never sees the plaintext password after the request handler. `UserManager.CreateAsync(user, password)` accepts the plaintext, runs the configured password validators (length, complexity, banned-password lists), hashes the result, and stores only the hash. `SignInManager.PasswordSignInAsync` does the equivalent on the way back: it reads the hash, asks the hasher to verify the supplied plaintext against it, and discards the plaintext as soon as the comparison completes.

### Lockout, registration, and password reset

Identity's lockout state is a small set of columns on the user record: a counter for consecutive failures, a `LockoutEnd` timestamp, and a flag for whether lockout is enabled at all. After `MaxFailedAccessAttempts` consecutive failures (default 5), `SignInManager` sets `LockoutEnd` to "now plus `DefaultLockoutTimeSpan`" (default 5 minutes). Subsequent sign-in attempts during the lockout window fail immediately without checking the password, even when the password is correct. A successful sign-in resets the counter. This blunts online password-guessing attacks: an attacker can try at most a few passwords per account per lockout window.

Registration is the inverse of sign-in: `UserManager.CreateAsync(user, password)` runs validators, hashes the password, persists the new row, and returns success or a list of validation errors. The application typically sends an email-confirmation link at this point, generated with `UserManager.GenerateEmailConfirmationTokenAsync` — a signed, time-limited token that the confirmation endpoint verifies before flipping the `EmailConfirmed` flag.

Password reset follows the same shape. `UserManager.GeneratePasswordResetTokenAsync` produces a token that is emailed to the user; the reset endpoint takes the token and a new password, calls `UserManager.ResetPasswordAsync`, and the framework verifies the token, runs the password validators, hashes the new password, and replaces the stored hash. Because the token is bound to the user's security stamp, any successful password change invalidates outstanding tokens — a useful defence when a previous reset link leaks.

## A worked example: registering and signing in a user

The companion exercises in [Identity and user stores](/exercises/10-webapp-development/5-identity-and-user-stores/) build a full registration and sign-in flow on top of the cookie foundation from the previous chapter. Figure 3 shows the registration handler in its smallest useful form.

Figure 3: A registration controller using `UserManager`

```csharp
public class RegisterController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public RegisterController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Candidate");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }
}
```

The handler reads two services from constructor injection. `CreateAsync` validates the password against the configured rules, generates a per-user salt, runs PBKDF2, and persists the hashed row through the EF user store. `AddToRoleAsync` writes a row to the `AspNetUserRoles` join table. `SignInAsync` hands the freshly created user to the cookie-authentication handler, which builds a `ClaimsPrincipal` from the user record, encrypts a ticket containing it, and emits the `Set-Cookie` header. The browser keeps the cookie; every subsequent request reaches the application as an authenticated session, and `HttpContext.User` exposes the principal exactly as in the hand-rolled example from the previous chapter — but without any of the password-handling code in the application's own source.

## When not to use Identity

Identity is the right default when the application owns its users — the user database belongs to this service, registration happens here, and password recovery flows live in this codebase. It is the wrong default in three situations.

| Situation | Better choice | Why |
|-----------|---------------|-----|
| Pure JSON API consumed by another service | JWT bearer authentication only | No browser, no sessions, no registration flow — Identity's machinery is dead weight. |
| Single-page app or mobile client federated to an external IdP | OpenID Connect (Microsoft Entra ID, Auth0, etc.) | The IdP owns the user database; the application validates tokens it produced. |
| Internal tool with no external users | Entra ID with conditional access | The organisation's existing directory holds the accounts; duplicating them in an app database is a liability. |

Each of these alternatives is covered in later chapters of this Part. The decision tree in practice is short: if the application stores its own user records and accepts passwords from those users, use Identity; otherwise, use the framework component that matches the credential format the application actually receives.

## Summary

ASP.NET Core Identity is the framework's user-management subsystem: it owns the user table, hashes passwords with PBKDF2 plus per-user salts, tracks lockout state, generates the tokens used for email confirmation and password reset, and exposes everything through two dependency-injected services — `UserManager<TUser>` for data and rules, `SignInManager<TUser>` for the sign-in flow. The user store interface decouples this logic from persistence, with EF Core against a relational database as the standard choice and custom implementations available when needed. Identity composes with cookie authentication rather than replacing it: cookies carry the session, Identity owns the database. Applications that store their own users almost always want Identity; APIs validating externally issued tokens, or systems federated to an external identity provider, do not.
