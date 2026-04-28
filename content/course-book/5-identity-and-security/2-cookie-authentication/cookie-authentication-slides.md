+++
title = "Cookie-Based Authentication and Sessions"
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

## Cookie-Based Authentication and Sessions
Part V — Identity and Security

---

## HTTP forgets every request
- HTTP itself carries **no memory** between requests
- A signed-in user must be re-recognised on every request
- The browser stores a **cookie** the server hands it
- Cookies travel automatically on every same-origin request

---

## The authentication cookie
- A **cookie** is data the server asks the browser to store and replay
- The cookie value is an **encrypted, signed authentication ticket**
- Opaque to the browser, tamper-evident to the server
- Editing one byte logs the user out, never escalates

---

## The sign-in handshake
- POST username and password over HTTPS
- Verify the password against the stored hash
- Build a `ClaimsPrincipal` and call `HttpContext.SignInAsync`
- Server emits `Set-Cookie`, browser stores it, redirects back

---

## Registering cookie authentication
- `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`
- `.AddCookie(options => …)` configures lifetime and security
- `app.UseAuthentication()` runs **before** `app.UseAuthorization()`
- The principal must reach `HttpContext.User` before `[Authorize]` checks

---

## Cookie attributes that matter
- `HttpOnly` — JavaScript cannot read the cookie
- `Secure` — sent only over HTTPS
- `SameSite=Lax` — withheld from most cross-site posts
- `Expires` / `Max-Age` — bounds the cookie's lifetime

---

## Sessions and sliding expiration
- A **session** is the span over which the same user is recognised
- Fixed expiration logs users out at a wall-clock time
- **Sliding expiration** re-emits the cookie as the user works
- Sign-out emits an expired cookie; idle sessions then die on their own

---

## CSRF — the cookie-specific threat
- The browser attaches the cookie to **every** request, including cross-site posts
- A hostile form can act as the user without ever reading the cookie
- Bearer-token APIs are unaffected — tokens are not auto-attached
- Defence lives at the **application layer**

---

## The anti-forgery token
- An **anti-forgery token** is a per-session secret in a hidden form field
- Paired with a separate cookie carrying the matching half
- `[ValidateAntiForgeryToken]` rejects any POST whose halves disagree
- `SameSite=Lax` plus the token is belt-and-braces

---

## Questions?
