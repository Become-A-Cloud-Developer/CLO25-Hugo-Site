+++
title = "4. Authentication and Authorization"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Cookie authentication, roles, claims, and policy-based authorization in ASP.NET Core without ASP.NET Core Identity"
weight = 4
+++

# Authentication and Authorization

Learn the authentication and authorization primitives of ASP.NET Core from the bottom up — cookies, `ClaimsPrincipal`, the `[Authorize]` attribute, and named policies — without the abstraction of ASP.NET Core Identity.

All four exercises evolve a single **"Who Am I?"** page that reveals what the server currently knows about you. Each exercise adds another column of insight: first your name, then your roles, then every claim on your identity, and finally a claim issued by an external provider.

> ℹ **What's deferred until Chapter 5**
>
> These exercises use a hardcoded in-memory user list and stay entirely in the presentation layer. Persistence, registration, password hashing, and ASP.NET Core Identity all arrive in Chapter 5 once you understand the primitives they build on.

{{< children />}}
