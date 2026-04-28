+++
title = "Pagination, Idempotency, and Rate Limiting"
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

## Pagination, Idempotency, and Rate Limiting
Part VI — Services and APIs

---

## Why returning everything fails
- A million-row response **wastes bandwidth, memory, and threads**
- Clients cannot resume a dropped connection mid-transfer
- The contract is **set on day one** — reversing it breaks clients
- Pagination caps response size from the first version of the API

---

## Three pagination strategies
- **Offset / limit** — `?offset=20&limit=10`, easy but deep pages slow
- **Page / size** — `?page=3&size=10`, UI-friendly, same trade-offs
- **Cursor** — opaque server-issued token, **stable under writes**
- Cursor cannot jump to an arbitrary page; offset can

---

## The response envelope
- JSON envelope with `data` and `pagination` fields, or
- GitHub-style **`Link` header** with `rel="next"`
- Envelope maps cleanly to a typed **DTO**
- Pick one and stay consistent across the API

---

## Idempotency
- A request is **idempotent** when sending it twice equals once
- `GET`, `PUT`, `DELETE` are idempotent by HTTP definition
- `POST` is **not** — two posts create two resources
- Idempotency lets client libraries retry safely on transient failures

---

## The Idempotency-Key header
- Client generates a UUID **before the first attempt**
- Same key on every retry of the same logical operation
- Server caches the response by key (24-hour TTL is common)
- Subsequent hits return the **cached response**, not a re-execution

---

## Rate limiting
- Caps requests per identity per time window
- Protects backends from **abuse and accidental hot-loops**
- Returns `429 Too Many Requests` with **`Retry-After`** header
- ASP.NET Core ships rate-limiter middleware out of the box

---

## Three rate-limit algorithms
- **Fixed window** — cheap, but allows 2× burst at the boundary
- **Sliding window** — smooth, more expensive to track
- **Token bucket** — bursts up to capacity, steady refill rate
- Token bucket fits **real bursty client behaviour** best

---

## Worked example
- `GET /api/quotes?cursor=...&limit=20`
- Returns `{ data: [...], pagination: { nextCursor: "..." } }`
- `Program.cs` registers a **token-bucket** policy: 100 tokens, 10/s refill
- `[EnableRateLimiting("quotes")]` opts the controller in

---

## Questions?
