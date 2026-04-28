+++
title = "DTOs vs Entities"
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

## DTOs vs Entities
Part VI — Services and APIs

---

## Two shapes, one resource
- The **entity** is the domain shape — what internal code reasons about
- The **DTO** is the wire shape — what the API exposes
- They look identical on day one, diverge by month three
- Different rates of change, different reasons to break
- One type for both is the choice that costs the most later

---

## Why the entity is a poor wire shape
- **Over-exposes** internal fields — every public property is published by default
- **Leaks persistence concerns** — navigation properties, `RowVersion`, key choices
- **Couples** the API contract to the database schema
- Adding a column ships the column to every client
- Hiding fields with `[JsonIgnore]` inverts the safe default

---

## What a DTO is
- A class shaped for **the wire**, not for storage
- Carries only the fields the API contract exposes
- No persistence attributes, no behaviour — a flat record
- Decoupled from the entity by a deliberate type boundary
- Database changes do not break clients; API changes do not force migrations

---

## Request DTO — input validation
- Describes what the client is **allowed to send**
- Omits server-controlled fields (`Id`, `CreatedAt`)
- Validation attributes attach naturally: `[Required]`, `[StringLength]`
- `[ApiController]` returns `400 Bad Request` automatically on failure
- Request rules and domain invariants stay independent

---

## Response DTO — selective exposure
- Describes what the server **returns**
- Internal columns stay out by construction — no property to fill
- Can reshape data: flatten, rename, format dates
- Carries server-assigned values like `Id` and `CreatedAt`
- Compiler enforces the asymmetry between request and response

---

## Where mapping lives
- **Inline** in the controller — fine for one resource, duplication grows
- **Dedicated mapper class** or extension methods — testable, one place to look
- **Library** (AutoMapper, Mapster) — zero per-type code, hidden behaviour
- The exercise uses a `private static QuoteDto ToDto(Quote q)` method
- Pick the lightest form that survives the resource count

---

## Worked example — Quote
- Entity carries `Id`, `Author`, `Text`, `CreatedAt`, plus future audit columns
- `CreateQuoteRequest` has `Author` and `Text` — server owns the rest
- `QuoteResponse` carries every public field, nothing internal
- Mapper is one method, changes only when the response shape changes
- Each layer evolves on its own clock

---

## The cost — when worth paying
- More types means more typing — three or four files per resource
- Worth it when the API is **public-facing** or has external clients
- Worth it when persistence will grow internal columns
- Worth it when validation rules differ from domain invariants
- Hard to justify only for thin private endpoints inside one deployable

---

## Questions?
