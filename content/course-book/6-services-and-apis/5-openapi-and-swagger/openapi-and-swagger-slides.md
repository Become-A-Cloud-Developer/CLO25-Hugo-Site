+++
title = "OpenAPI and Swagger"
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

## OpenAPI and Swagger
Part VI — Services and APIs

---

## The contract problem
- An API without machine-readable docs forces clients to **read source or guess**
- Hand-written docs **drift** the moment a controller changes
- Source code is authoritative but unreadable to non-developers
- The fix: a contract derived from the code, in a format **tools** can consume

---

## OpenAPI specification
- **OpenAPI** is the standard JSON or YAML format describing an HTTP API
- Declares paths, operations, parameters, schemas, security schemes
- Donated to the Linux Foundation in 2015; OpenAPI 3.0 is the common target
- One document — read by humans, code generators, and contract testers

---

## Paths, operations, schemas
- A **path** is a URI template like `/api/Quotes/{id}`
- An **operation** is an HTTP method on a path — `GET /api/Quotes/{id}`
- A **schema** describes a JSON shape, usually a DTO
- Reusable schemas live under `components/schemas` and are referenced by `$ref`

---

## Security schemes in the document
- Bearer token: `"type": "http", "scheme": "bearer"`
- API key: `"type": "apiKey", "in": "header", "name": "X-Api-Key"`
- Tooling treats security as a first-class part of the contract
- Code generators emit a credential parameter; testers fail if auth is missing

---

## Swagger UI as the browser explorer
- **Swagger UI** renders an OpenAPI document as a navigable, executable page
- One tag per controller; expand to see operations and schemas
- **Try it out** turns each operation into a form that issues a real request
- Served at `/swagger` by Swashbuckle in ASP.NET Core

---

## Swashbuckle generates the document
- Introspects controllers, route attributes, DTOs, return types
- `AddEndpointsApiExplorer()` + `AddSwaggerGen()` register the generator
- `UseSwagger()` serves the JSON; `UseSwaggerUI()` serves the explorer
- Both must register **before** `MapControllers()`

---

## Annotations make the document richer
- `[ProducesResponseType(typeof(QuoteDto), 200)]` documents the success body
- `[ProducesResponseType(404)]` documents the failure path
- XML doc comments flow into operation and property descriptions
- Default-generated docs are correct but sparse — annotations close the gap

---

## Exposing auth so Authorize works
- `AddSecurityDefinition("Bearer", ...)` declares the bearer scheme
- `AddSecurityRequirement(...)` attaches it to operations
- Swagger UI grows an **Authorize** button; a pasted JWT flows into every "Try it out"
- Without it, secured routes return `401` from inside the explorer

---

## The toolchain payoff
- **Code generators** (NSwag, Kiota) emit typed client SDKs
- **Contract testers** (Schemathesis) assert responses match schemas
- **Postman / Insomnia** import the document as a request collection
- **Mock servers** (Prism) serve example responses for parallel development

---

## Questions?
