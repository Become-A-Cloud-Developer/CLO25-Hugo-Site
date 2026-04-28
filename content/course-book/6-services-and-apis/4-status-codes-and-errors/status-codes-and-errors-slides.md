+++
title = "Status Codes, Versioning, and Error Responses"
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

## Status Codes, Versioning, and Error Responses
Part VI — Services and APIs

---

## The status code is the primary signal
- A REST call has **two languages** — body for data, status for outcome
- Clients should branch on the **code alone**, not the prose
- A `200` with an error message in the body breaks every parser
- The body adds detail; the **status line tells the truth**

---

## The success codes
- **`200 OK`** — the unmarked default for `GET` and successful `PUT`
- **`201 Created`** — `POST` made a new resource; include a **`Location`** header
- **`204 No Content`** — success with nothing to return; typical for `DELETE`
- The body of a `201` should be the **new resource**, not just an id

---

## The client-error codes
- **`400 Bad Request`** — malformed input, missing field, parse failure
- **`401 Unauthorized`** — no valid credential; really means *unauthenticated*
- **`403 Forbidden`** — credential is valid but **not entitled**
- **`404 Not Found`** — the resource at this URI does not exist
- **`409 Conflict`** — request collides with **current state** of the resource

---

## 400 vs 422 and the throttling code
- **`400`** — JSON did not parse, types wrong, required field missing
- **`422 Unprocessable Entity`** — JSON parsed, **business rule** rejected it
- **`429 Too Many Requests`** — rate-limit exceeded; include `Retry-After`
- `[ApiController]` defaults to `400` for model validation — pick one and stay consistent

---

## Server errors and information leakage
- **`500 Internal Server Error`** — unhandled exception, downstream failure
- The body must **never expose** stack traces or internal exception messages
- Anonymous clients should not learn the system's internal shape
- Map exceptions to `ProblemDetails` in `app.UseExceptionHandler`

---

## Problem details — RFC 7807
- A standard JSON error shape: `type`, `title`, `status`, `detail`, `instance`
- Returned with `Content-Type: application/problem+json`
- One parser works for **every** API that follows the convention
- `type` is the stable kind-of-error key; `instance` correlates to logs

---

## ProblemDetails in ASP.NET Core
- `ControllerBase.Problem(...)` — base helper for any error
- `ControllerBase.ValidationProblem(...)` — adds the **`errors`** dictionary
- `[ApiController]` returns `ValidationProblemDetails` on model failures **automatically**
- Pair with `app.UseExceptionHandler("/error")` for unhandled exceptions

---

## API versioning strategies
- **URL path** — `/v1/quotes`; visible, easy to route, the safe default
- **Header (media type)** — `Accept: ...; v=2`; clean URI, harder to debug
- **Query string** — `?api-version=2`; easy to pin, clutters caches
- Pick **one** and apply it across every endpoint

---

## Breaking-change discipline
- Most changes should be **additive** — new optional fields, new endpoints
- A new field never breaks an existing client; a renamed one always does
- Breaking changes go behind a **new version**, with the old kept alive
- Use `Sunset: <date>` to tell clients when the old version retires

---

## Questions?
