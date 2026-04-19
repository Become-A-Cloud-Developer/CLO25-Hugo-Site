+++
title = "Week 3"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Authentication and authorization in ASP.NET Core: Identity, cookies, roles, and CSRF protection"
weight = 3
+++

# Week 3: Authentication and Authorization

This week covers the identity layer of the reference application. You will learn the conceptual split between authentication and authorization, see how ASP.NET Core Identity issues cookies, and gate controllers with the `[Authorize]` attribute.

## Presentation

- **[Authentication & Authorization](/presentations/auth-authz.html)** — Lecture covering AuthN/AuthZ concepts, OAuth 2.0, OIDC, and the ASP.NET Core implementation

## Theory

- Authentication vs authorization — the conceptual split
- Principals, credentials, identities, and claims
- Authentication methods (password, token, MFA) and types (local, remote, managed)
- OAuth 2.0 and OpenID Connect
- ASP.NET Core Identity, cookie authentication, and JWT authentication
- `[Authorize]`, roles, claims, and CSRF protection

## Practice

- Scaffold ASP.NET Core Identity into the reference application
- Configure cookie-based login and logout
- Protect controllers with `[Authorize]` and role filters
- Verify CSRF protection on state-changing POST endpoints

## Preparation

- Read up on the difference between authentication and authorization
- Skim the ASP.NET Core Identity documentation

## Reflection Questions

- What is the difference between authentication and authorization?
- How does cookie-based login work in a web application?
- Why is CSRF protection needed, and how is it implemented in ASP.NET Core?
