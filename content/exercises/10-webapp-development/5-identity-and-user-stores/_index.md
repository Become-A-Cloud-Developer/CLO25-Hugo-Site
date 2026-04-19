+++
title = "5. Identity and User Stores"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Introduce ASP.NET Core Identity, swap between InMemory and SQLite user stores with a feature flag, seed the first admin from secrets, and add registration with role promotion"
weight = 5
+++

# Identity and User Stores

Chapter 4 built cookie authentication from first principles. Now that you understand the `ClaimsPrincipal` abstraction, **ASP.NET Core Identity** takes over the plumbing: user persistence, password hashing, role management, and a registration flow.

The exercises in this chapter progressively layer Identity into the existing project:

1. **Introducing ASP.NET Core Identity** — replace the hand-rolled `AccountController` with `SignInManager` backed by EF Core InMemory.
2. **User Store: InMemory vs SQLite** — feature-flag the persistence so the same app can use either store, chosen by configuration.
3. **Seeding the First Admin** — config-driven idempotent seeder that works for both stores; the hardcoded test users from Exercise 5.1 become a proper bootstrap.
4. **Registration and Role Promotion** — unlock Identity's registration UI, default new users to Candidate, and let admins promote them.

{{< children />}}
