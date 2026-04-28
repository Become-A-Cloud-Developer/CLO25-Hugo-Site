+++
title = "REST Principles"
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

## REST Principles
Part VI — Services and APIs

---

## Why REST exists
- HTTP APIs are contracts between programs that have **never met**
- Without a shared style, every API invents its own conventions
- **REST** is the dominant architectural style on top of HTTP
- Articulated by Roy Fielding in 2000 as a description of how the web already worked
- Not a protocol — a set of **constraints** a service can choose to honour

---

## What REST is
- **REST** = Representational State Transfer
- The server exposes named **resources** at URIs
- The client manipulates **representations** with the standard HTTP methods
- Each request carries everything the server needs to process it
- A service is more or less RESTful — there is no pass/fail

---

## The five load-bearing constraints
- **Client-server separation** — independent halves, only the API contract between them
- **Statelessness** — every request self-contained; no server-side session
- **Cacheability** — responses declare whether they can be cached
- **Uniform interface** — resources, standard methods, representations
- **Layered system** — proxies and CDNs are invisible to the client

---

## Statelessness in practice
- The server keeps **persistent** state — no per-conversation state
- Any replica can serve any request
- Auth context, cursors, identifiers all ride **on every request**
- The client gets bigger; the server scales horizontally for free
- Trade-off: bigger payloads, repeated headers, no implicit context

---

## The uniform interface
- **Resource** = anything addressable by a URI (`/quotes/42`)
- **Representation** = the bytes on the wire (usually JSON)
- Standard methods: `GET`, `POST`, `PUT`, `PATCH`, `DELETE`
- Conventions let intermediaries reason without knowing the app
- HATEOAS (links in responses) is the strict-REST aspiration — rarely shipped

---

## Worked example: GET /quotes/42
- `GET /api/quotes/42` names the resource
- `200 OK` with `Content-Type: application/json` is the representation
- `Cache-Control: public, max-age=60` makes it cacheable
- No session cookie — pure **statelessness**
- Built in the companion exercise's `CloudCiApi`

---

## Richardson Maturity Model
- **Level 0** — one URL, action in the body (SOAP-over-HTTP)
- **Level 1** — multiple URLs, but still POST for everything
- **Level 2** — resources + standard methods + status codes (most "REST" APIs)
- **Level 3** — Level 2 + HATEOAS (rare in production)
- Level 2 is the sweet spot for working APIs

---

## REST is not the only choice
- **gRPC** — HTTP/2 + Protocol Buffers, smaller and streamable, opaque to HTTP tooling
- **GraphQL** — client picks fields from one endpoint, hard to cache
- **REST** — wins for public HTTP APIs, CDN caching, predictable resources
- Pick by workload, not by hype

---

## Questions?
