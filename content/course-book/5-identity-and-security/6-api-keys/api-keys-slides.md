+++
title = "API Keys and Machine-to-Machine"
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

## API Keys and Machine-to-Machine
Part V — Identity & Security

---

## The machine-to-machine problem
- A backend calling another backend has **no user**
- No login form, no browser, no cookie to carry
- The caller is a process, not a person
- Still needs a credential the server can check
- API keys are the cheapest gate for this case

---

## What an API key is
- A long, opaque **shared secret** between client and server
- Sent in a request header — conventionally `X-Api-Key`
- Carries **no claims**, no identity, no expiration
- Proves only "the caller knows the secret"
- Same property for every client that holds the key

---

## How it travels with a request
- Header on every call: `X-Api-Key: kQ8j+7Hf...`
- Must travel over **TLS** — plaintext puts it in router logs
- No header → `401 Unauthorized` + `WWW-Authenticate: ApiKey`
- Wrong header → `403 Forbidden`
- Right header → request continues to the controller

---

## ASP.NET Core middleware
- A class with a `RequestDelegate` constructor and `InvokeAsync(HttpContext)`
- Registered with `app.UseMiddleware<ApiKeyMiddleware>()`
- Sits **after** routing, **before** controllers
- Reads the header, compares with `string.Equals(..., Ordinal)`
- Either short-circuits or calls `await _next(context)`

---

## Weaker than JWT
- **No expiration** — valid until configuration changes
- **No claims** — every caller looks identical
- **No per-caller authorization** — same key, same access
- **Rotation is destructive** unless the server accepts two keys
- JWT carries identity; API keys carry only "knows the secret"

---

## Generation and storage
- Generate with `openssl rand -base64 48` — 384 bits of entropy
- 256 bits is the floor; the real risk is **leakage**, not brute force
- **Never** in source control — committed once means leaked forever
- Local dev: non-secret placeholder in `appsettings.Development.json`
- Production: platform secret store + `secretref:` env var

---

## Key rotation
- Single-key rotation is destructive — every client breaks at once
- Production pattern: accept **`Current` and `Previous`** in parallel
- Promote new key, demote old; clients cut over at their own pace
- Remove `Previous` once telemetry shows no traffic on the old key
- Without rotation, every leak is permanent

---

## When an API key is enough
- One trusted client deployed alongside the server
- Threat model is "keep anonymous traffic out," not per-caller audit
- Internal or low-sensitivity APIs where simplicity outweighs attribution

---

## When it is not
- Per-caller attribution required ("which client did this?")
- Different callers need different permissions
- Credentials must expire without operator action
- Multiple independent consumers must be revoked individually
- The next step is a **JWT bearer token**

---

## Questions?
