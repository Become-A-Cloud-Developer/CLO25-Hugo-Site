+++
title = "Week 3"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Authentication and authorization in ASP.NET Core: cookie auth primitives, roles, claims, policies, CSRF, and ASP.NET Core Identity"
weight = 3
+++

# Week 3: Authentication and Authorization

This week covers the identity layer of the reference application from the bottom up. You'll first build cookie authentication by hand to understand the primitives, then move up to ASP.NET Core Identity for a production-shaped user-management layer.

## Presentation

- **[Authentication & Authorization](/presentations/auth-authz.html)** — Lecture covering AuthN/AuthZ concepts, OAuth 2.0, OIDC, and the ASP.NET Core implementation

## Theory

- Authentication vs authorization — the conceptual split
- Principals, credentials, identities, and claims
- Authentication methods (password, token, MFA) and types (local, remote, managed)
- OAuth 2.0 and OpenID Connect
- ASP.NET Core Identity, cookie authentication, and JWT authentication
- `[Authorize]`, roles, claims, policies, and CSRF protection

## Practice

The practice lands in two chapters under the Webapp Development exercises:

- **[Chapter 4 — Authentication and Authorization](/exercises/10-webapp-development/4-authentication-authorization/)** — four exercises that build cookie auth, roles, claims, policies, CSRF, and Google OIDC on top of a minimal scaffolded app, using hardcoded test users only. Stays entirely in the presentation layer.
- **[Chapter 5 — Identity and User Stores](/exercises/10-webapp-development/5-identity-and-user-stores/)** — four further exercises that replace the hand-rolled sign-in with ASP.NET Core Identity, add a feature-flagged InMemory/SQLite user store, introduce config-driven admin seeding, and finish with registration and role promotion.

All exercises evolve a single **"Who Am I?"** page that reveals exactly what the server knows about the current user — authentication state, name, scheme, roles, full claims table, and (after the Google exercise) provider-issued claims.

## Reference project

The starter for all eight exercises lives at `reference/CloudSoft-Auth/` alongside `CloudSoft-Newsletter` and `CloudSoft-Recruitment`. A complete, evolving reference implementation is tracked in the git history of this repository; each exercise end-state has its own commit.

End-to-end Playwright tests cover every stage (except the live Google OIDC round-trip). Run them with `./run-playwright-tests.sh headless` (InMemory) or `./run-playwright-tests-sqlite.sh headless` (SQLite).

## Preparation

- Read up on the difference between authentication and authorization
- Skim the ASP.NET Core Identity documentation

## Reflection Questions

- What is the difference between authentication and authorization?
- How does cookie-based login work in a web application?
- Why is CSRF protection needed, and how is it implemented in ASP.NET Core?
- When does ASP.NET Core Identity pay for itself over hand-rolled cookie auth?
- How should the first admin user get into a fresh system?
