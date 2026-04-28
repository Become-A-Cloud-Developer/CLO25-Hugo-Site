# Part VI — Glossary

Terminology contract for the six chapters of Part VI — Services and APIs.

## Terms owned by this Part

### REST
- **Owner chapter**: `1-rest-principles`
- **Canonical definition**: **REST** (Representational State Transfer) is an architectural style for HTTP APIs in which the server exposes named resources, the client manipulates representations of those resources using the standard HTTP methods, and each request carries everything the server needs to process it.
- **Used by chapters**: 1 (owner), 2, 3, 4, 5

### Resource
- **Owner chapter**: `1-rest-principles`
- **Canonical definition**: A **resource** is anything an API addresses with a URI — a customer record, a list of orders, a single image — and that the API can return a representation of, accept updates to, or delete.
- **Used by chapters**: 1 (owner), 2

### Statelessness
- **Owner chapter**: `1-rest-principles`
- **Canonical definition**: **Statelessness** is the REST constraint that each request from a client must contain all the information the server needs to process it; the server keeps no client session between requests, which makes the API trivially horizontally scalable but pushes session state into the client or into a separate token.
- **Used by chapters**: 1 (owner)

### Resource collection
- **Owner chapter**: `2-resource-modeling`
- **Canonical definition**: A **resource collection** is a URI that addresses a list of resources of the same type (`/quotes`, `/orders`); it is conventionally manipulated with `GET` to list, `POST` to create, and `DELETE` only when removing the entire collection makes sense.
- **Used by chapters**: 2 (owner)

### Resource item
- **Owner chapter**: `2-resource-modeling`
- **Canonical definition**: A **resource item** is a URI that addresses one specific resource by its identifier (`/quotes/42`, `/orders/abc-123`); manipulated with `GET` to retrieve, `PUT` or `PATCH` to modify, and `DELETE` to remove.
- **Used by chapters**: 2 (owner)

### DTO
- **Owner chapter**: `3-dtos-vs-entities`
- **Canonical definition**: A **DTO** (Data Transfer Object) is a class shaped specifically for travelling over the wire between client and server; it carries only the fields the API contract exposes, and is decoupled from the persistence model so that database changes do not break clients and API changes do not require database migrations.
- **Used by chapters**: 3 (owner), 4, 5

### Entity
- **Owner chapter**: `3-dtos-vs-entities`
- **Canonical definition**: An **entity** is the in-memory class representing a row in the database (or a document in a document store); its shape is driven by persistence concerns — primary keys, navigation properties, audit columns — that the API client neither needs nor should see.
- **Used by chapters**: 3 (owner)

### Mapper
- **Owner chapter**: `3-dtos-vs-entities`
- **Canonical definition**: A **mapper** is the code that converts between an entity and a DTO; it can be hand-written extension methods, an automated library such as AutoMapper, or constructor-style records that take an entity and emit a DTO.
- **Used by chapters**: 3 (owner)

### API versioning
- **Owner chapter**: `4-status-codes-and-errors`
- **Canonical definition**: **API versioning** is the practice of letting old clients keep working when the API contract changes; common strategies are URL-path versioning (`/v1/quotes`), media-type versioning (`Accept: application/vnd.api+json; v=2`), and query-string versioning (`?api-version=2`).
- **Used by chapters**: 4 (owner)

### Problem details
- **Owner chapter**: `4-status-codes-and-errors`
- **Canonical definition**: **Problem details** (RFC 7807) is the standard JSON shape for HTTP error responses: an object with `type`, `title`, `status`, `detail`, and `instance` properties, returned with `Content-Type: application/problem+json`; ASP.NET Core emits this shape from `ProblemDetails` and `ValidationProblemDetails` helpers.
- **Used by chapters**: 4 (owner)

### OpenAPI
- **Owner chapter**: `5-openapi-and-swagger`
- **Canonical definition**: **OpenAPI** is the standard format (JSON or YAML) for describing an HTTP API's endpoints, request and response shapes, parameters, and authentication schemes; tooling consumes the OpenAPI document to generate documentation, client SDKs, and contract tests.
- **Used by chapters**: 5 (owner), 6

### Swagger UI
- **Owner chapter**: `5-openapi-and-swagger`
- **Canonical definition**: **Swagger UI** is the browser-rendered explorer that reads an OpenAPI document and displays each endpoint with its parameters and request/response examples, with a "Try it out" button that issues real requests; in ASP.NET Core it is added by the Swashbuckle package and served at `/swagger`.
- **Used by chapters**: 5 (owner)

### Pagination
- **Owner chapter**: `6-pagination-and-rate-limiting`
- **Canonical definition**: **Pagination** is the practice of returning a slice of a large collection per request rather than the whole collection; common strategies are offset-based (`?offset=20&limit=10`), page-based (`?page=3&size=10`), and cursor-based (a server-issued opaque token that is opaque to the client).
- **Used by chapters**: 6 (owner)

### Idempotency
- **Owner chapter**: `6-pagination-and-rate-limiting`
- **Canonical definition**: **Idempotency** is the property that calling the same operation with the same input multiple times produces the same observable result as calling it once; `GET`, `PUT`, and `DELETE` are idempotent by HTTP definition, while `POST` is generally not — for at-least-once delivery to be safe, an `Idempotency-Key` header lets the server detect retries.
- **Used by chapters**: 6 (owner)

### Rate limiting
- **Owner chapter**: `6-pagination-and-rate-limiting`
- **Canonical definition**: **Rate limiting** is the server-side practice of capping how many requests a client can make in a window; common algorithms are fixed window, sliding window, and token bucket; ASP.NET Core ships a rate-limiting middleware that returns `429 Too Many Requests` when a client exceeds its quota.
- **Used by chapters**: 6 (owner)

## Terms borrowed from earlier Parts

### HTTP / Request / Response / Header / Method / Status code / URI
- **Defined in**: Part III — Application Development / `1-http-fundamentals`
- **Reference link**: `/course-book/3-application-development/1-http-fundamentals/`

### MVC / Controller / Action / Routing / Model binding
- **Defined in**: Part III — Application Development / `3-the-mvc-pattern`
- **Reference link**: `/course-book/3-application-development/3-the-mvc-pattern/`

### Dependency injection
- **Defined in**: Part III — Application Development / `6-dependency-injection`
- **Reference link**: `/course-book/3-application-development/6-dependency-injection/`

### Bearer token / JWT
- **Defined in**: Part V — Identity & Security / `5-bearer-tokens-and-jwt`
- **Reference link**: `/course-book/5-identity-and-security/5-bearer-tokens-and-jwt/`

### API key
- **Defined in**: Part V — Identity & Security / `6-api-keys`
- **Reference link**: `/course-book/5-identity-and-security/6-api-keys/`
