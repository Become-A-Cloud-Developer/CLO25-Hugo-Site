+++
title = "Roles, Claims, and Policies"
program = "CLO"
cohort = "25"
courses = ["BCD", "ACD"]
type = "slide"
date = 2026-04-28
draft = false
hidden = true

theme = "sky"
[revealOptions]
controls = true
progress = true
history = true
center = true
+++

## Roles, Claims, and Policies
Part V — Identity & Security

---

## Authorization needs facts
- Authentication confirms **who** signed in
- Authorization decides **what** that identity may do
- Decisions need **facts about the user**, not just a yes-to-signed-in
- Facts live on the principal as **claims**

---

## The ClaimsPrincipal
- `HttpContext.User` exposes a **`ClaimsPrincipal`**
- Each **claim** is a name-value pair (type + value)
- Controllers read claims via `User.FindFirst(type)`
- The cookie ticket carries the same claims back on every request

---

## Roles are a special claim
- A **role** is a claim of type `ClaimTypes.Role`
- `[Authorize(Roles = "Admin")]` checks for that claim
- `User.IsInRole("Admin")` is the same check by hand
- Coarse on purpose — fast group membership lookup

---

## Worked example: AdminController
- `[Authorize(Roles = "Admin")]` on the controller class
- Every action requires the **Admin** role claim
- Anonymous request → 401, redirected to login
- Authenticated non-admin → 403 forbidden

---

## When roles stop being enough
- "Edit your own profile" depends on **identity**, not group
- "EU tenant" depends on a **tenant claim**
- "At least 18" depends on a **computation**
- Encoding nuance as roles bloats the role list

---

## Authorization policies
- An **authorization policy** is a named set of requirements
- Registered once via `AddAuthorization(o => o.AddPolicy(...))`
- Applied with `[Authorize(Policy = "Name")]`
- Renaming or rewriting the rule is a **one-line edit**

---

## Custom requirements and handlers
- `IAuthorizationRequirement` carries the parameters
- `IAuthorizationHandler` evaluates against `context.User`
- Handler calls `context.Succeed(requirement)` on match
- Example: **MinimumAge** policy reads a `DateOfBirth` claim

---

## A decision framework
- Plain `[Authorize]` — any authenticated user
- `[Authorize(Roles = "X")]` — rule is exactly group membership
- `RequireClaim` policy — rule is exactly claim equality
- Custom requirement + handler — rule needs computation

---

## Questions?
