+++
title = "ASP.NET Core Identity"
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

## ASP.NET Core Identity
Part V — Identity & Security

---

## Why not roll your own?
- Password hashing, salts, and iteration counts are easy to get **wrong**
- Reset tokens, email confirmation, and lockout are full features in themselves
- Every reinvention is a future **security incident**
- The framework already ships a tested implementation

---

## What Identity is
- A library for **user management**: registration, sign-in, reset, lockout
- Owns the user table and the **password hash**
- Composes with cookie authentication — does not replace it
- Two main services: **UserManager** and **SignInManager**

---

## The IdentityUser model
- Base entity with `Id`, `UserName`, `Email`, `PasswordHash`, `SecurityStamp`
- Tracks **lockout** state and failed-attempt counter
- **Extend** by deriving `ApplicationUser : IdentityUser`
- The derived type flows through as a generic parameter

---

## User stores
- A **user store** is the persistence backend behind Identity
- **EF Core** against SQL is the standard choice
- **In-memory** store for tests and demos only
- **Custom** stores possible via `IUserStore<TUser>` for legacy databases

---

## UserManager vs SignInManager
- **UserManager** is the data API — create, find, change password, add to role
- **SignInManager** orchestrates the **sign-in flow** end to end
- `PasswordSignInAsync` validates the hash and issues the cookie ticket
- Both are **DI-registered**, request-scoped services

---

## Password hashing in Identity
- Default algorithm: **PBKDF2** with HMAC-SHA-512
- **Per-user salt** plus 310,000 iterations (current default)
- Slowness defeats offline attacks; salt defeats rainbow tables
- Application **never stores plaintext** — only the hash

---

## Lockout and reset flows
- **Lockout** activates after `MaxFailedAccessAttempts` (default 5)
- Locked accounts fail fast even with a correct password
- **Reset tokens** are signed and time-limited; bound to the security stamp
- Successful password change invalidates outstanding tokens

---

## Worked example
- `AddDefaultIdentity<IdentityUser>().AddEntityFrameworkStores<ApplicationDbContext>()`
- Register controller calls `_userManager.CreateAsync(user, password)`
- Then `_userManager.AddToRoleAsync(user, "Candidate")`
- Finally `_signInManager.SignInAsync(user, isPersistent: false)`

---

## When not to use Identity
- Pure JSON APIs — use **JWT bearer** only
- SPA federated to an external IdP — use **OpenID Connect**
- Internal tools — use organisation's directory (Entra ID)
- Use Identity when the application owns its **user database**

---

## Questions?
