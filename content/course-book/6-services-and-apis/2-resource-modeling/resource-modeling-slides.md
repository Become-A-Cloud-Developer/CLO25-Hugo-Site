+++
title = "Resource Modeling and URIs"
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

## Resource Modeling and URIs
Part VI — Services and APIs

---

## The unit of API design
- REST APIs expose **resources**, not functions
- First question is the noun, not the verb
- HTTP methods supply the verbs
- Predictability comes from one consistent grid
- Clients can guess the next URI without reading the docs

---

## Collection vs item
- **Collection** — `/quotes` addresses the list
- **Item** — `/quotes/{id}` addresses one
- Methods on a collection: `GET`, `POST`, rarely `DELETE`
- Methods on an item: `GET`, `PUT`, `PATCH`, `DELETE`
- Two shapes carry almost every endpoint

---

## Naming conventions
- **Plural noun** for collections — `/quotes`, never `/quote`
- **No verbs** in URIs — the HTTP method is the verb
- **Lowercase**, hyphens between words — `/customer-orders`
- Identifiers in item URIs — int, GUID, or slug
- Whatever you pick becomes part of the contract

---

## Hierarchical relationships
- `/customers/{id}/orders` — orders belonging to one customer
- Sub-resource shape says "child belongs to parent"
- Flat shape `/orders?customerId=42` says "child is independent"
- Both can return identical JSON
- The URI emphasises *what the API thinks the data is*

---

## Decision framework
- **Sub-resource** when child cannot exist without parent
- **Sub-resource** when authorization scope matches the parent
- **Flat with filter** when cross-parent queries are common
- **Flat with filter** when the child has a global identity
- Some APIs expose both — scoped list, flat canonical address

---

## Filtering and pagination
- Query parameters narrow a collection — `?status=active`
- Filtering, sorting, search — all `?` not `/`
- Pagination is the same idea — `?page=3&size=20`
- `/quotes/page/3` is a URI mistake
- Detailed pagination patterns come at the end of this Part

---

## URI stability — the contract
- A published URI is a contract — clients depend on it
- Renaming `/quotes` to `/cool-quotes` breaks every client
- The remedy when shape must change is **API versioning**
- `/v1/quotes` stays frozen; `/v2/quotes` is the new shape
- Ten minutes at the whiteboard saves the breaking change later

---

## CloudCiApi worked example
- `GET /api/quotes` — list, returns array of `QuoteDto`
- `GET /api/quotes/{id}` — one item, or `404`
- `POST /api/quotes` — `201 Created` with `Location` header
- `[HttpGet("{id:int}")]` route constraint pins segment to ints
- `CreatedAtAction(...)` builds the URL from the named action

---

## What good resource modeling buys
- Guessable URIs — new endpoints feel like the old ones
- Stable URIs — security changes touch middleware, not URLs
- Clear ownership — sub-resource vs flat is explicit
- Predictable grid — clients map operations to methods
- The rest of Part VI builds on top of this shape

---

## Questions?
