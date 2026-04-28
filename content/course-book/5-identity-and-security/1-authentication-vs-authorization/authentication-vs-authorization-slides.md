+++
title = "Authentication vs Authorization"
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

## Authentication vs Authorization
Part V — Identity & Security

---

## Two questions every secured request answers
- **Who** is making this call?
- **Are they allowed** to do what they ask?
- Authentication answers the first
- Authorization answers the second
- Same request, different sources of truth

---

## Authentication
- Validates a **credential** against a known identity
- Inputs: password, bearer token, certificate
- Output: an identity attached to the request
- Failure → `401 Unauthorized`
- Says nothing about what the caller may do

---

## Authorization
- Decides whether an **authenticated identity** may proceed
- Inputs: the principal and the requested resource
- Reads roles, claims, or policy results
- Failure → `403 Forbidden`
- Never touches the credential

---

## The principal
- A `ClaimsPrincipal` attached to `HttpContext.User`
- Carries a name and a list of **claims**
- Built by the authentication handler
- Read by every authorization check
- Same shape regardless of credential format

---

## Pipeline order
- **Authentication middleware** runs early — populates `HttpContext.User`
- **Routing** matches the request to a controller action
- **Authorization filter** evaluates `[Authorize]` against the principal
- Action runs only if the filter passes

---

## Authorization decisions
- **Role check** — `[Authorize(Roles = "Admin")]`
- **Claim check** — `User.HasClaim("tenant", id)`
- **Policy evaluation** — `[Authorize(Policy = "ManagerOnly")]`
- Coarse to fine, simple to centralized

---

## What this Part develops
- **Cookies** — browser sessions
- **ASP.NET Core Identity** — managed user store
- **Claims and policies** — authorization rules
- **JWT** — bearer tokens for APIs
- **OAuth / OIDC** — federated identity
- **Key Vault** — operational secrets

---

## Questions?
