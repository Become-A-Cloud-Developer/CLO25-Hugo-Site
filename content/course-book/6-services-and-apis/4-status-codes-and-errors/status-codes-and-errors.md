+++
title = "Status Codes, Versioning, and Error Responses"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/6-services-and-apis/4-status-codes-and-errors.html)

[Se presentationen på svenska](/presentations/course-book/6-services-and-apis/4-status-codes-and-errors-swe.html)

---

A REST API speaks two languages at once. The body carries the data the client asked for or sent in, but the *outcome* of the call is communicated by the response code, the headers, and — when something goes wrong — a structured error body. Clients written by different teams, in different languages, on different release schedules need a way to know whether to retry, give up, prompt the user, or fall over to a different code path; reading prose from a freeform `message` field is not it. Three things an API author has to get right let every client answer "what just happened?" mechanically: the status code returned on each path, the standard shape of an error body, and the discipline that lets the contract evolve without breaking the clients that depend on it.

## Status codes as the primary signal

The general meaning of HTTP status codes — the 1xx through 5xx ranges, the canonical names, the cacheability rules — is developed in [HTTP fundamentals](/course-book/3-application-development/1-http-fundamentals/). What matters here is the narrower set an API author actually reaches for and the conventions for choosing among them. A REST API is a finite vocabulary of resource manipulations, and a small, well-disciplined subset of [status codes](/course-book/3-application-development/1-http-fundamentals/) is enough to describe every outcome a client needs to reason about.

The principle is that the status code is the *primary* signal. The body adds detail, but the client should be able to branch on the code alone. A `200` followed by an error message in the body, or a `500` whose body says "everything is fine," forces every client to read prose to find the truth. An API that always tells the truth in the status line is one that any HTTP library, monitoring agent, or load balancer can interpret without a custom parser.

### The success codes

Three codes cover almost every successful response.

**`200 OK`** is the unmarked default. The request succeeded; the body holds whatever the client asked for. `GET /api/quotes` returns `200` with a JSON array; `GET /api/quotes/42` returns `200` with one object; a successful `PUT` that overwrote an existing resource returns `200` with the updated representation.

**`201 Created`** is the response to a `POST` that created a new resource. Two things are mandatory: the body should be the new resource (so the client does not have to make a follow-up `GET`), and the response must include a `Location` header pointing at the canonical URI of the new resource. The `Location` header is the wire-level answer to "where did it go?" — clients that need to bookmark, share, or later fetch the resource read it from there rather than constructing the URL themselves.

**`204 No Content`** signals success with no body. The typical caller is a `DELETE` that succeeded — there is nothing meaningful to return — or a `PUT` that the client does not need a response payload for. Returning `204` rather than `200` with an empty body is more than cosmetic: it tells the client there is nothing to parse, and intermediaries (caches, proxies) treat it accordingly.

### The client-error codes

The 4xx range tells the client "you got something wrong; do not retry without changing the request." Five codes carry almost all the load.

**`400 Bad Request`** is the catch-all for malformed input. The JSON did not parse, a required field was missing, a value was out of range. ASP.NET Core's `[ApiController]` attribute returns `400` automatically when model validation fails — the controller action never runs.

**`401 Unauthorized`** means the request lacked a valid credential. The name is historical; "unauthenticated" would be more accurate. The convention is that `401` invites the client to retry with credentials, and the response should include a `WWW-Authenticate` header naming the scheme expected (`Bearer`, `Basic`, an API key header).

**`403 Forbidden`** means the credential was valid but the caller is not allowed to perform this action. The user is signed in but lacks the role; the API key is recognised but not entitled to the endpoint. `403` is terminal in a way that `401` is not — retrying with the same credential will give the same `403`.

**`404 Not Found`** means the resource at this URI does not exist. The route matched, but the entity is not there — `GET /api/quotes/9999` when there is no quote 9999. `404` is also the conventional response when the caller is not entitled to *know* whether the resource exists; revealing the difference between "not found" and "forbidden" can leak information.

**`409 Conflict`** means the request collided with the current state of the resource. Two clients edited the same record and the second `PUT` arrived with a stale version; a `POST` tried to create a resource with a unique key that another resource already holds. `409` invites the client to re-fetch, reconcile, and try again.

### Validation: 400 versus 422

The 4xx codes draw a line between *malformed* and *semantically invalid*. **`422 Unprocessable Entity`** was introduced to express the second case: the JSON parsed, the field types were correct, but the values violate a business rule (a quote `Author` longer than 100 characters, an order with a negative quantity). `400` is the older, broader code, and many APIs use it for both cases without confusing clients. `[ApiController]` defaults to `400` for model validation failures; either choice is defensible as long as it is consistent.

### Throttling and server errors

**`429 Too Many Requests`** is the response when the client has exceeded a rate limit. The response should include a `Retry-After` header naming how many seconds the client must wait before trying again. Rate limiting itself is developed in the [pagination and rate limiting chapter](/course-book/6-services-and-apis/6-pagination-and-rate-limiting/); what matters here is that `429` is a distinct signal from `503` ("server unavailable") and from `403` ("not allowed at all").

**`500 Internal Server Error`** is the catch-all for unexpected failures. The action threw an exception the application did not handle, the database is unreachable, a downstream dependency timed out. `500` is the only code in this list whose response body must not contain a stack trace, exception message, or any other internal detail — error responses returned to anonymous clients should never expose information that helps an attacker map the system.

## Problem details: a standard error shape

When a request fails, the client needs more than a status code. It needs to know *which* field was wrong, *why* the conflict occurred, *what* the rate-limit window is. The temptation is to invent a custom JSON envelope per service — `{ "error": "...", "code": 1234, "details": [...] }` — and every team has done it once. The result is that every client that talks to two APIs has to write two parsers.

**Problem details** (RFC 7807) is the standard JSON shape for HTTP error responses: an object with `type`, `title`, `status`, `detail`, and `instance` properties, returned with `Content-Type: application/problem+json`; ASP.NET Core emits this shape from `ProblemDetails` and `ValidationProblemDetails` helpers. The five fields have stable meanings:

- `type` — a URI identifying the *kind* of error. Stable across calls, useful as a key for client-side switch logic.
- `title` — a short human-readable summary of the kind. Stable across calls; safe to log or show to a developer.
- `status` — the HTTP status code, repeated in the body for clients that have already lost the response line.
- `detail` — a human-readable explanation specific to this occurrence. May include the offending value.
- `instance` — a URI identifying *this specific* occurrence, useful for correlating with server-side logs.

A typical `404` response from an ASP.NET Core API following the convention looks like this:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "No quote exists with id 9999.",
  "instance": "/api/quotes/9999",
  "traceId": "00-7f3c...-01"
}
```

The shape is open: properties beyond the five standard ones are allowed. ASP.NET Core adds `traceId` so that an error in production can be matched to a specific request in the logs without the client having to invent a correlation header.

### `ProblemDetails` and `ValidationProblemDetails` in ASP.NET Core

ASP.NET Core ships two strongly-typed helpers that produce this shape. `ProblemDetails` is the base class with the five standard fields; `ValidationProblemDetails` extends it with an `errors` dictionary that maps field names to arrays of validation messages. Controllers reach them through `ControllerBase.Problem(...)` and `ControllerBase.ValidationProblem(...)`.

The framework also wires them in by default. `[ApiController]` returns a `ValidationProblemDetails` body automatically when model binding fails, and `app.UseExceptionHandler("/error")` paired with a controller that returns `Problem(...)` produces a `ProblemDetails` body for unhandled exceptions. The author writes the happy path; the framework writes the error path.

## A worked example: create and validation failure

The companion exercise [REST Controllers, DTOs, and Swagger](/exercises/4-services-and-apis/1-rest-api-and-dtos/) builds a `QuotesController` whose `Create` action illustrates both signals — the `201 Created` with a `Location` header on success, and the `400` with a `ValidationProblemDetails` body on validation failure.

```csharp
[HttpPost]
public ActionResult<QuoteDto> Create([FromBody] CreateQuoteRequest request)
{
    if (request.Text.Contains("forbidden-word"))
    {
        return ValidationProblem(
            detail: "The quote text contains a disallowed term.",
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    var quote = _store.Add(request.Author, request.Text);
    var dto = ToDto(quote);

    return Created($"/api/quotes/{quote.Id}", dto);
}
```

Three response paths leave this method. A request with valid input runs the last two lines: `Created(...)` produces a `201`, sets the `Location` header to `/api/quotes/<id>`, and returns the DTO as the body. A request whose `Text` is missing or longer than 500 characters never reaches the method body — `[ApiController]` runs model validation first and returns `400` with a `ValidationProblemDetails` body listing the offending fields. A request that *parses* but trips a business rule reaches the method, and `ValidationProblem(...)` returns `422` with the same problem-details shape. Three different outcomes, three different status codes, one consistent error envelope. A client that checks the status code first and falls through to the `errors` dictionary on `400` and `422` handles every case the same way.

## API versioning

Once an API has clients in production, the contract is frozen — at least the parts those clients use. Adding fields to a response is usually safe; renaming a field, removing one, changing a status code, or restructuring an error body is not. **API versioning** is the practice of letting old clients keep working when the API contract changes; common strategies are URL-path versioning (`/v1/quotes`), media-type versioning (`Accept: application/vnd.api+json; v=2`), and query-string versioning (`?api-version=2`).

Three families of strategy show up in production APIs. Each is a defensible answer; the trade-offs are in routing, caching, and client ergonomics.

| Strategy | Example | Strengths | Trade-offs |
|----------|---------|-----------|------------|
| URL path | `/v1/quotes`, `/v2/quotes` | Visible in logs, in browser history, in `curl`; trivial to route | Couples versioning to the URI, which conflicts with the REST view that the URI names a resource, not a representation |
| Header (media type) | `Accept: application/vnd.acme+json; v=2` | Keeps the URI clean; aligns with content negotiation | Invisible in logs and bookmarks; awkward to test from a browser; harder to cache by URL |
| Query string | `/quotes?api-version=2` | Easy to pin per request; visible in logs | Mixes versioning with normal query parameters; clutters cache keys |

URL-path versioning is what most teaching examples use and what most public APIs ship: it is unambiguous, easy to explain, and tooling friendly. Header-based versioning is purer and what the hypermedia community prefers. The choice matters less than picking one and applying it consistently across every endpoint.

### Breaking-change discipline

Versioning is the escape hatch, not the default. The discipline that keeps APIs healthy is treating *most* changes as additive: a new optional field on a response, a new optional query parameter, a new endpoint at a fresh URI. Additive changes do not need a version bump because no existing client breaks.

Some changes are unavoidably breaking: removing a field, changing its type, narrowing what the server accepts, repurposing a status code. The rule for those changes is to introduce them under a new version, keep the old version running long enough for clients to migrate, and document the deprecation window — a header like `Sunset: Wed, 31 Dec 2025 23:59:59 GMT` lets the server tell the client when the old version stops working. Removing a version before its sunset is the failure mode that makes "we have versioning" feel like a lie to the teams that depend on it.

## Summary

The status code is the primary signal an API gives its clients: a small disciplined vocabulary (200, 201, 204, 400, 401, 403, 404, 409, 422, 429, 500) is enough to describe every outcome that matters. The body of an error response should follow RFC 7807 problem details — `type`, `title`, `status`, `detail`, `instance` — with `Content-Type: application/problem+json`, so every client written against any API can use the same parser; ASP.NET Core ships `ProblemDetails` and `ValidationProblemDetails` plus `[ApiController]` to produce this shape automatically. API versioning is the escape hatch for the changes that cannot be made additively: pick one strategy (URL path is the safest default), keep additions backward compatible, and document the sunset date when an old version retires. The combined effect is a contract that any client can interpret mechanically and that the server team can evolve without breaking the callers it has already shipped to.
