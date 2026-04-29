+++
title = "Week 3 (v.17)"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Authentication and authorization in ASP.NET Core: cookie auth primitives, roles, claims, policies, CSRF, and ASP.NET Core Identity"
weight = 3
+++

# Week 3 (v.17) — Authentication and Authorization

Cover the identity layer of the reference application from the bottom up. Build cookie authentication by hand to understand the primitives, then move up to ASP.NET Core Identity for a production-shaped user-management layer.

## Presentation

- [Authentication & Authorization](/presentations/auth-authz.html) — lecture covering AuthN/AuthZ concepts, OAuth 2.0, OIDC, and the ASP.NET Core implementation

## Theory

- [Part V — Identity and Security](/course-book/5-identity-and-security/)
  - [Authentication vs Authorization](/course-book/5-identity-and-security/1-authentication-vs-authorization/authentication-vs-authorization/)
  - [Cookie Authentication](/course-book/5-identity-and-security/2-cookie-authentication/cookie-authentication/)
  - [ASP.NET Core Identity](/course-book/5-identity-and-security/3-aspnet-core-identity/aspnet-core-identity/)
  - [Roles, Claims, and Policies](/course-book/5-identity-and-security/4-roles-claims-and-policies/roles-claims-and-policies/)
  - [OAuth and OIDC](/course-book/5-identity-and-security/7-oauth-and-oidc/oauth-and-oidc/)

## Practice

The practice lands in two chapters under the Webapp Development exercises:

- [Chapter 4 — Authentication and Authorization](/exercises/10-webapp-development/4-authentication-authorization/) — four exercises that build cookie auth, roles, claims, policies, CSRF, and Google OIDC on top of a minimal scaffolded app, using hardcoded test users only. Stays entirely in the presentation layer.
- [Chapter 5 — Identity and User Stores](/exercises/10-webapp-development/5-identity-and-user-stores/) — four further exercises that replace the hand-rolled sign-in with ASP.NET Core Identity, add a feature-flagged InMemory/SQLite user store, introduce config-driven admin seeding, and finish with registration and role promotion.

All exercises evolve a single **"Who Am I?"** page that reveals exactly what the server knows about the current user — authentication state, name, scheme, roles, full claims table, and (after the Google exercise) provider-issued claims.

## Preparation

- Read up on the difference between authentication and authorization
- Skim the ASP.NET Core Identity documentation

## Reflection Questions

- What is the difference between authentication and authorization?
- How does cookie-based login work in a web application?
- Why is CSRF protection needed, and how is it implemented in ASP.NET Core?
- When does ASP.NET Core Identity pay for itself over hand-rolled cookie auth?
- How should the first admin user get into a fresh system?

## Links

- [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
