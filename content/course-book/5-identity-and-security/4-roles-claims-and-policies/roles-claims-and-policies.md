+++
title = "Roles, Claims, and Policies"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/5-identity-and-security/4-roles-claims-and-policies.html)

[Se presentationen på svenska](/presentations/course-book/5-identity-and-security/4-roles-claims-and-policies-swe.html)

---

Once a request has been authenticated, the application still has to decide whether the caller may perform the requested action. That decision needs facts about the caller — which department they belong to, which roles they hold, how old they are, which tenant they signed in from — not merely confirmation that they signed in. ASP.NET Core represents those facts as claims hanging off a `ClaimsPrincipal`, and offers three layers of authorization built on that representation: role checks, named policies, and custom requirements with their own handlers. Each layer earns its place at a different level of rule complexity, illustrated below by an admin-only controller and a side policy that reads a date-of-birth claim.

## Claims as the unit of fact

A **claim** is a name-value pair carried by a `ClaimsPrincipal` that asserts something about the identity — name, email, role membership, tenant, scope; authorization decisions read claims off the principal rather than re-querying the user database. Each claim has a type (a URI-like string such as `"role"`, `"email"`, or a custom `"DateOfBirth"`) and a value (the asserted fact, such as `"Admin"` or `"1995-04-12"`). Authentication is what populates the principal with claims; authorization is what reads them.

This separation matters because it lets authorization run cheaply on every request. The cookie-authentication middleware decrypts the auth ticket and reconstructs the principal once; from then on the controller, the action filter, and the view all see the same claim set without touching the database. Claims travel with the request, not with the data store.

### How a controller sees claims

Inside a controller, `User` is the `ClaimsPrincipal`. The principal exposes its claims through `User.Claims` (an enumerable) and `User.FindFirst(type)` (a lookup). The framework's [Authorize] attribute, introduced with the MVC pattern in Part III, hooks into the same principal: it asks the principal whether it satisfies the requirement attached to the attribute and short-circuits with a 401 or 403 if not.

```csharp
public IActionResult Profile()
{
    string? name = User.Identity?.Name;
    string? email = User.FindFirst("email")?.Value;
    string? dateOfBirth = User.FindFirst("DateOfBirth")?.Value;
    return View(new ProfileViewModel(name, email, dateOfBirth));
}
```

The action does not call into the user database. Every fact it displays was placed on the principal by the sign-in flow — either by the cookie middleware deserialising the auth ticket, or by a claim transformation step that ran when the principal was constructed.

## Roles as a special claim

A **role** is a named group that an identity belongs to (Admin, Candidate, Reviewer); the framework treats role membership as a special claim and provides shorthand syntax such as `[Authorize(Roles = "Admin")]` to require it. Despite the convenience syntax, there is no magic: a role is a claim whose type is `ClaimTypes.Role` (the URI `"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"`, also accepted as the short form `"role"`). The shorthand `User.IsInRole("Admin")` walks the principal's claims and returns true if any has type `Role` and value `"Admin"`.

Role-based authorization is coarse on purpose. It answers a single question — does the user belong to this group? — and it answers it quickly. For long-lived organizational categories that map directly to UI sections, that is exactly the right tool.

### The admin-only controller

Consider a `CloudSoft Recruitment Portal` admin area: pages to approve candidates, retire job postings, and manage other administrators. None of these actions should be reachable by a regular candidate or even by a recruiter. A role check on the controller class enforces the boundary in one line:

```csharp
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult Dashboard() => View();

    public IActionResult ApproveCandidate(int id)
    {
        // ... business logic ...
        return RedirectToAction(nameof(Dashboard));
    }
}
```

Every action in `AdminController` now requires an authenticated principal whose claim set contains a `Role` claim with value `"Admin"`. An anonymous request bounces to the login page (401 unauthorized, redirected by the cookie scheme); an authenticated non-admin gets a 403 forbidden. The check applies uniformly without each action having to repeat itself.

The same attribute accepts a comma-separated list to widen the gate: `[Authorize(Roles = "Admin,Recruiter")]` admits anyone with either role. Multiple `[Authorize]` attributes stack with AND semantics — `[Authorize(Roles = "Admin")] [Authorize(Roles = "Recruiter")]` would require both, which is rarely useful.

## When roles stop being enough

Role checks fail the moment the rule depends on something other than group membership. "Users may edit their own profile but not other people's" depends on identity equality, not a role. "Users from the EU tenant must accept GDPR terms" depends on a tenant claim. "Users must be at least 18" depends on a date. Encoding these as roles works once and then collapses: every nuance becomes a new role, and the role list grows until it is no longer a list of groups but a list of policies in disguise.

Claim-based checks address the first half of the problem by reading specific claims directly: `User.HasClaim("tenant", "eu")`. They are flexible but ad hoc — the rule lives wherever it is needed, scattered across actions, with no single name to refer to. That makes the rules hard to audit and even harder to change consistently. The third layer, **policies**, gives the rule a name and a single registration point.

## Authorization policies

An **authorization policy** is a named set of requirements registered at startup; controllers and actions opt in with `[Authorize(Policy = "Name")]`, and the framework evaluates each requirement against the principal's claims to decide allow or deny. The policy is registered once in `Program.cs`, named once, and applied wherever the rule matters. If the rule changes, it changes in one place.

For simple cases, the policy can be expressed inline using the fluent builder on `AuthorizationOptions`:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EuTenant", policy =>
        policy.RequireClaim("tenant", "eu"));

    options.AddPolicy("AdminOrRecruiter", policy =>
        policy.RequireRole("Admin", "Recruiter"));
});
```

A controller action then references the policy by name:

```csharp
[Authorize(Policy = "AdminOrRecruiter")]
public IActionResult Inbox() => View();
```

The shift from `[Authorize(Roles = "Admin,Recruiter")]` to `[Authorize(Policy = "AdminOrRecruiter")]` looks cosmetic, but the indirection is the point. The action no longer encodes the rule; it asks for "the rule we call AdminOrRecruiter," and the rule's definition lives in `Program.cs`. Renaming a role, swapping the rule for "Admin OR holds a `recruiter-permission` claim," or routing the decision through an external system all become one-line edits at the registration point — the controllers do not change.

### A custom requirement: minimum age

`RequireClaim` and `RequireRole` cover claim-equality checks. Anything more interesting — comparisons, computations, conditional logic — needs a custom requirement and a handler. The pattern has two pieces: an `IAuthorizationRequirement` (a marker carrying any parameters the rule needs) and an `IAuthorizationHandler` (the code that evaluates whether the principal satisfies the requirement).

```csharp
public class MinimumAgeRequirement : IAuthorizationRequirement
{
    public int MinimumAge { get; }

    public MinimumAgeRequirement(int minimumAge) => MinimumAge = minimumAge;
}

public class MinimumAgeHandler : AuthorizationHandler<MinimumAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MinimumAgeRequirement requirement)
    {
        Claim? dobClaim = context.User.FindFirst("DateOfBirth");
        if (dobClaim is null || !DateTime.TryParse(dobClaim.Value, out var dob))
        {
            return Task.CompletedTask;
        }

        int age = DateTime.UtcNow.Year - dob.Year;
        if (dob.Date > DateTime.UtcNow.AddYears(-age))
        {
            age--;
        }

        if (age >= requirement.MinimumAge)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

The handler reads the principal's `DateOfBirth` claim, parses it, computes the user's age, and calls `context.Succeed(requirement)` only if the age clears the threshold. It never calls `context.Fail()` — the framework's default is failure, so a handler that does not succeed leaves the requirement unmet. That convention lets multiple handlers contribute to a single requirement, succeeding under different conditions, without one silently vetoing another.

The requirement and handler are wired up at startup:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MinimumAge18", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));
});

builder.Services.AddSingleton<IAuthorizationHandler, MinimumAgeHandler>();
```

A controller action gates itself on the policy without knowing how the rule is implemented:

```csharp
[Authorize(Policy = "MinimumAge18")]
public IActionResult AdultContent() => View();
```

The action expresses intent ("require an adult"); the handler expresses mechanism (parse a date claim, compute years). Each can change without the other, and the policy name keeps the two halves connected.

## A decision framework

Each authorization style has a sweet spot. Choosing well keeps rules readable and changeable; choosing poorly buries the real logic in scattered conditionals or in role names that have stopped describing groups.

| Mechanism | Use when | Trade-off |
|-----------|----------|-----------|
| `[Authorize]` (no arguments) | Action requires any authenticated user | Says nothing about who; relies on a separate authentication step |
| `[Authorize(Roles = "X")]` | Rule is exactly "is the user in group X" | No way to express anything beyond group membership |
| `RequireClaim("type", "value")` policy | Rule is exactly "user has this fact" | Still equality-only; no comparisons or computations |
| Custom `IAuthorizationRequirement` + handler | Rule needs computation, comparison, or external lookup | More code; the rule lives in C#, not declarations |

Two questions sharpen the choice. First: does the rule have a name a colleague would recognise? If yes, prefer a named policy regardless of how simple it currently is — the name itself is the long-term value. Second: can the rule be expressed as "the principal has claim X with value Y"? If yes, `RequireClaim` is enough; if not, a custom requirement is the honest expression.

Roles remain useful for the high-traffic, organization-shaped categories (Admin, Customer, Support). Claims-and-policies become essential the moment rules involve resource ownership, attributes of the resource itself, or anything time-dependent. Most production applications use both: roles for the broad strokes, policies for the nuance.

## Where claims come from

A policy can only check claims that the principal actually carries. The cookie middleware reconstructs the principal from the encrypted auth ticket — which means whatever claims were placed on the principal at sign-in time are the claims policies will see. Adding a new claim type therefore has two costs: updating the sign-in code to issue it, and accepting that previously issued cookies will not contain it until users sign in again.

ASP.NET Core Identity, covered in [its own chapter](/course-book/5-identity-and-security/3-aspnet-core-identity/), provides a `IUserClaimsPrincipalFactory<TUser>` extension point: when `SignInManager` builds the principal, it asks the factory to layer additional claims on top of the user record's stored claims. That is where a `DateOfBirth` claim would be added in an Identity-backed application — the factory reads the field off the `ApplicationUser` and emits it as a claim every time the user signs in. Claim transformation, registered as `IClaimsTransformation`, runs even later (on every request) and is suited to short-lived enrichments — for example, looking up the user's current subscription tier without baking it into the cookie.

The companion exercise [Authentication and Authorization](/exercises/10-webapp-development/4-authentication-authorization/) builds these layers from the bottom: a hardcoded user list signs in with cookies, the principal is given a role claim, an `[Authorize(Roles = "Admin")]` page enforces it, and a final exercise issues a date-of-birth claim and gates an adult-content page on a `MinimumAge` policy. Walking through each step manually — without ASP.NET Core Identity in the way — makes the relationship between claim, role, and policy concrete.

## Summary

Claims are the unit of fact authorization runs on, hanging off the `ClaimsPrincipal` that ASP.NET Core attaches to every authenticated request. Roles are a special-cased claim type with shorthand syntax, ideal for coarse organizational categories such as the admin-only `AdminController`. Named policies give rules a single registration point and a recognisable name, replacing scattered `User.HasClaim` calls with `[Authorize(Policy = "...")]`. When a rule needs computation, comparison, or anything beyond claim equality — a `MinimumAge` check against a date-of-birth claim is the canonical example — a custom `IAuthorizationRequirement` paired with an `IAuthorizationHandler` keeps the logic in one place while the controllers stay declarative. Roles for the broad strokes, policies for the nuance, custom handlers for the rules a single claim cannot express: each layer earns its place, and most applications use them together.
