+++
title = "OAuth 2.0 and OpenID Connect"
program = "CLO"
cohort = "25"
courses = ["ACD"]
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

## OAuth 2.0 and OpenID Connect
Part V — Identity & Security

---

## The delegated-access problem
- Apps must call APIs on a user's behalf
- Asking for a third-party password is unsafe and impractical
- "Sign in with Google" needs identity without password sharing
- Solution: a trusted **authorization server** holds the credential
- Tokens carry scoped permission and signed identity

---

## The four actors
- **Resource owner** — the human user
- **Client** — the application requesting access
- **Authorization server** — Entra ID, Google, GitHub, Auth0
- **Resource server** — the API hosting the user's data
- Trust between them is carried by signed tokens

---

## Grant types at a glance
- **Authorization code** — interactive user login (browser apps)
- **Client credentials** — machine-to-machine, no user
- **Device code** — input-constrained devices (TVs, CLIs)
- **Refresh token** — exchange long-lived token for a fresh access token
- Implicit and password grants are discouraged

---

## Authorization code with PKCE
- Front channel: browser redirect to authorization server
- User logs in there, consents to **scopes**
- Server redirects back with a one-time `code`
- Back channel: client exchanges `code` + `code_verifier` for tokens
- PKCE binds the two halves — defends public clients

---

## Redirect URI and state
- **Redirect URI** must be pre-registered, exact match
- Prevents attackers from redirecting codes to themselves
- **state** parameter — random per-login value
- Validated on callback to defeat CSRF
- Both checks are non-optional

---

## What OIDC adds
- OAuth alone returns an **access token** — a permission slip
- OIDC returns an **ID token** — a signed JWT asserting *who*
- ID token claims: `sub`, `name`, `email`, `iss`, `aud`
- Client validates the signature against the issuer's public key
- ID token is for the client; access token is for the API

---

## OAuth vs OIDC
- **OAuth** answers authorization — "may this client call that API?"
- **OIDC** answers authentication — "who is this user?"
- Same protocol family, different questions
- Workload federation (GitHub Actions → Azure) reuses OIDC for non-human callers
- Covered in the DevOps part of the book

---

## ASP.NET Core registration
- `AddOpenIdConnect` registers the handler
- `Authority` points to the issuer's discovery URL
- `ClientId` and `ClientSecret` from app registration
- `ResponseType = "code"`, `UsePkce = true`
- Result: a `ClaimsPrincipal` populated from the ID token

---

## Questions?
