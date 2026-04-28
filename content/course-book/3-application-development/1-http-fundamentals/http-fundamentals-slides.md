+++
title = "HTTP Fundamentals"
program = "CLO"
cohort = "25"
courses = ["BCD"]
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

## HTTP Fundamentals
Part III — Application Development

---

## Why HTTP Exists
- Layered on **TCP/IP** — adds meaning to the byte stream
- **Text-based** messages — readable with `curl` and dev tools
- **Stateless** — each request stands alone, state travels in headers
- **Uniform interface** — same vocabulary for every application

---

## The Request-Response Cycle
- Client opens a TCP connection to host and port
- Client writes a **request** — method, URI, headers, optional body
- Server writes a **response** — status code, headers, optional body
- Connection is freed; the next request repeats the shape

---

## HTTP Methods
- **GET** — retrieve a representation, no side effects
- **POST** — submit data that creates or mutates state
- **PUT** — replace a resource at a known URI
- **PATCH** — partial update of a resource
- **DELETE** — remove a resource

---

## GET versus POST
- **GET**: parameters in query string, body empty, bookmarkable
- **POST**: parameters in body, hidden from URL bar and history
- **GET** is **safe** and **idempotent** — repeats are harmless
- **POST** is neither — repeats create duplicates
- Mutating actions always use **POST** (or `PUT`, `PATCH`, `DELETE`)

---

## Status Codes
- **1xx** informational, **2xx** success, **3xx** redirect
- **4xx** client error — request is wrong, retry will not help
- **5xx** server error — server failed, retry may succeed
- Common: `200 OK`, `302 Found`, `400 Bad Request`, `404 Not Found`, `500 Internal Server Error`

---

## URIs and Headers
- **URI** = scheme + authority + path + query + fragment
- **Path** drives routing to a controller action
- **Query string** carries `GET` parameters as `key=value` pairs
- **Headers** carry metadata: `Content-Type`, `Authorization`, `Cookie`, `Cache-Control`

---

## Worked Example: curl to ASP.NET Core
- `curl -v https://localhost:7240/Home/Index`
- Request: `GET /Home/Index HTTP/1.1` plus `Host` and `Accept` headers
- Server matches path to `HomeController.Index()`
- Response: `200 OK`, `Content-Type: text/html`, rendered Razor view in body
- A `POST` form submission carries body data and typically redirects with `302`

---

## Idempotency Matters on Failure
- Retrying a `GET`, `PUT`, or `DELETE` after a timeout is safe
- Retrying a `POST` may create duplicates — payment APIs use idempotency keys
- The "Confirm form resubmission" dialog exists because `POST` is not idempotent

---

## Questions?
