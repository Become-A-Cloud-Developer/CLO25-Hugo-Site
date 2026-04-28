+++
title = "Bearer Tokens and JWT"
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

## Bearer Tokens and JWT
Part V — Identity & Security

---

## Cookies do not fit every caller
- A **mobile app** has no shared cookie jar with a browser
- A **single-page app** crosses origins where cookies struggle
- A **service-to-service** call has no browser at all
- All of them can still set an HTTP **header**

---

## What "bearer" means
- Sent on each request as `Authorization: Bearer <token>`
- The server validates the token, not who is presenting it
- **Whoever holds the token** is treated as authenticated
- Therefore: **TLS is mandatory**, lifetimes stay short

---

## The JWT format
- Three base64-encoded segments joined by dots
- **Header** — algorithm and key ID (`alg`, `kid`)
- **Payload** — JSON claims about the identity
- **Signature** — proves the first two segments were not altered

---

## Standard claims in the payload
- `iss` — **issuer**, the service that signed the token
- `sub` — **subject**, the identity the token is about
- `aud` — **audience**, the service meant to accept it
- `exp`, `nbf`, `iat` — when the token is valid

---

## Validating a JWT
- Read `kid` from the header, look up the **verification key**
- **Symmetric** (HS256) — shared secret for issuer and validator
- **Asymmetric** (RS256) — issuer signs private, validators use public
- Check `iss`, `aud`, and `exp` after the signature passes

---

## ASP.NET Core JwtBearer
- `AddJwtBearer` with **Authority** and **Audience**
- Authority drives the public-key fetch from `/.well-known/...`
- `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`
- Controllers see the same **`ClaimsPrincipal`** as cookie auth

---

## Short access tokens, refresh tokens
- Access tokens live **minutes**, not days
- A leaked token is a usable credential until it expires
- A separate **refresh token** trades for a new access token
- Refresh tokens can be **revoked** server-side; access tokens cannot

---

## When JWT is the wrong choice
- A session can be **killed** in the database — the next call fails
- A JWT is **valid until it expires**, with no central check
- Block-lists and key rotation undo the format's main benefit
- Where revocation must be **immediate**, prefer server-side sessions

---

## Questions?
