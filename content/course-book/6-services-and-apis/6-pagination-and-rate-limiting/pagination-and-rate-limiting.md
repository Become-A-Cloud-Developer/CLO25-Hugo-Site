+++
title = "Pagination, Idempotency, and Rate Limiting"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 60
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/6-services-and-apis/6-pagination-and-rate-limiting.html)

[Se presentationen på svenska](/presentations/course-book/6-services-and-apis/6-pagination-and-rate-limiting-swe.html)

---

A REST API that runs against a real database eventually faces three operational pressures that the happy-path design ignored. The first is size: a `GET /quotes` that grew from ten rows in development to a million rows in production becomes hostile to every client that calls it. The second is duplication: a mobile client retries a `POST /orders` after a flaky network and the server has no way to tell whether it is seeing a new order or the second copy of the previous one. The third is abuse: a single client running a hot retry loop or a misconfigured cron job can saturate a backend that was sized for the average case. Pagination, idempotency, and rate limiting are the three contractual mechanisms that turn a working API into one that is also safe to expose. Each of them addresses one of the three pressures, and the worked example at the end ties them together with a paginated controller and a token-bucket policy in `Program.cs`.

## Why returning everything is not an option

A `GET` against a [resource collection](/course-book/6-services-and-apis/2-resource-modeling/) is the natural first endpoint to write. It is also the one most likely to collapse under its own weight. A collection with a million rows serialises to hundreds of megabytes of JSON, occupies a controller worker thread for the entire duration of the database read, and forces the client to receive, parse, and store all of it before it can render the first row. A connection that drops at the 90% mark wastes everything transferred so far, because there is no way to resume.

Returning everything is a contract decision, not a coding decision. Once the API promises that `GET /quotes` returns the entire collection, every client is entitled to that response forever, and the server has to keep finding the memory and bandwidth to produce it. Reversing the decision later means breaking clients. The remedy is to commit to returning only a slice from the very first version.

**Pagination** is the practice of returning a slice of a large collection per request rather than the whole collection; common strategies are offset-based (`?offset=20&limit=10`), page-based (`?page=3&size=10`), and cursor-based (a server-issued opaque token that is opaque to the client). All three strategies cap the per-request response size, but they differ in how they identify the next slice and in how they behave when the underlying collection is changing while the client is paging through it.

### Offset and limit

The offset-based strategy reads as plain English: "skip the first 20 rows, then give me 10". It maps cleanly onto the SQL `OFFSET 20 LIMIT 10` clause that most databases support natively, which is why it is the default in many starter tutorials. The trade-off is that the database still has to count and discard the skipped rows on every request, so deep pagination (`?offset=950000`) gets slower as the offset grows, even though the response size stays the same. The strategy also drifts under concurrent writes: if rows are inserted before the current offset between two page requests, the client either sees the same row twice or skips a row entirely.

### Page and size

Page-based pagination (`?page=3&size=10`) is offset-based pagination in friendlier clothes. The server computes `offset = (page - 1) * size` and runs the same query. The advantage is that page numbers are easier to render in a UI ("page 3 of 47") and easier for humans to reason about than raw row counts. The trade-offs are identical to offset-based: deep pages are slow, and the boundary drifts under concurrent writes. The strategy works best on collections that are mostly stable and where users are expected to land on a page directly rather than scroll through the whole list.

### Cursor-based pagination

Cursor-based pagination replaces the numeric offset with an opaque token that the server issues alongside each page. The client treats the cursor as a black box and passes it back unmodified on the next request. Internally, the cursor encodes the position of the last row returned — typically the primary key plus a sort field — so the server can resume the scan with `WHERE id > :cursor` instead of skipping rows. This makes deep pagination cheap (no skipping) and stable under inserts (a new row at the start of the collection does not shift the cursor's meaning). The trade-off is that cursors do not support arbitrary jumps: a client cannot ask for "page 47" because the cursor for that page has not been issued yet. Cursors also tie the client to the server's sort order, since the cursor is meaningless if the sort changes.

The three strategies map onto different read patterns:

| Strategy | Strength | Trade-off |
|----------|----------|-----------|
| Offset / limit | Easy to implement, supports arbitrary jumps | Deep pages slow, drifts under concurrent writes |
| Page / size | UI-friendly, intuitive for humans | Same trade-offs as offset / limit |
| Cursor | Stable under writes, deep pages cheap | No arbitrary jumps, server controls sort order |

### The response envelope

Once the response is paginated, the client needs enough information to fetch the next slice. Two conventions dominate. The first is a JSON envelope that wraps the data in an object with two top-level fields:

```json
{
  "data": [
    { "id": 1, "author": "Hopper", "text": "..." },
    { "id": 2, "author": "Knuth", "text": "..." }
  ],
  "pagination": {
    "nextCursor": "eyJpZCI6Mn0=",
    "limit": 20
  }
}
```

The second is the GitHub-style `Link` header, where the response body is a plain JSON array and the next-page URL is carried in an HTTP header (`Link: <https://api.example.com/quotes?cursor=...>; rel="next"`). The envelope shape is easier for typed clients (the `pagination` object maps cleanly to a [DTO](/course-book/6-services-and-apis/3-dtos-vs-entities/)), while the `Link` header keeps the response body shape symmetrical between paginated and non-paginated endpoints. Both are correct; consistency within an API matters more than the choice between them.

## Idempotency as a contract

A request is **idempotent** when sending it twice has the same observable effect as sending it once. The HTTP specification fixes this property for some methods and leaves it open for others, and clients rely on those guarantees when they retry on failure.

**Idempotency** is the property that calling the same operation with the same input multiple times produces the same observable result as calling it once; `GET`, `PUT`, and `DELETE` are idempotent by HTTP definition, while `POST` is generally not — for at-least-once delivery to be safe, an `Idempotency-Key` header lets the server detect retries.

`GET` is idempotent because reading a resource does not change it. `PUT` is idempotent because it replaces the resource with the request body, so applying the same body twice leaves the resource in the same state as applying it once. `DELETE` is idempotent because the second delete finds nothing to delete and returns `404` or `204` — the resource is gone either way. The HTTP method (defined in [Part III HTTP fundamentals](/course-book/3-application-development/1-http-fundamentals/)) carries the contract; client libraries and proxies are entitled to retry idempotent requests on transient failures without asking the application.

`POST` is the outlier. Posting `{ "amount": 100 }` to `/orders` twice creates two orders. This is correct semantics — the second `POST` is a different operation, even if the body is byte-identical — but it makes retries dangerous. A mobile client that times out on an order submission has no way to know whether the server received the request and the response was lost, or the request never arrived. Retrying could double-charge the customer; not retrying could lose the order.

### The Idempotency-Key header

The pattern that resolves this is the **`Idempotency-Key`** request header. The client generates a unique value (typically a UUID) before the first attempt, attaches it to the request, and reuses the same value on every retry. The server stores the key alongside the response it generated and, on a subsequent request with the same key, returns the cached response instead of executing the operation again.

```http
POST /api/orders HTTP/1.1
Idempotency-Key: 9c3f2b1e-0d7a-4e8a-9c01-f7c2a4d3b2a1
Content-Type: application/json

{ "amount": 100, "currency": "USD" }
```

The server's part is bookkeeping: persist the key with a TTL (24 hours is common), record the resulting status code and body, and return the cached response on a hit. Stripe popularised this pattern for payment APIs, and it is now the standard mechanism for safe `POST` retries. The trade-off is the storage cost of the key cache and the requirement that clients generate keys consistently — a client that generates a fresh UUID on every retry defeats the mechanism.

## Rate limiting as a backstop

A well-behaved client paginates and uses idempotency keys. A misbehaving or misconfigured client does neither and hammers the API with requests until something gives way. **Rate limiting** is the server-side practice of capping how many requests a client can make in a window; common algorithms are fixed window, sliding window, and token bucket; ASP.NET Core ships a rate-limiting middleware that returns `429 Too Many Requests` when a client exceeds its quota.

Rate limiting protects shared backend resources from abuse and from accidental hot-loops. A single client running a `while (true) { GET /quotes }` loop can exhaust the database connection pool, starve every other client, and crash the service. The rate limiter caps the damage that any single identity can do per unit of time, returning a `429` status code (defined in [Part III HTTP fundamentals](/course-book/3-application-development/1-http-fundamentals/)) once the cap is reached. The response carries a `Retry-After` header that tells the client how long to wait before its next attempt.

### The three algorithms

The **fixed window** algorithm divides time into equal buckets (say, one minute each) and counts requests per identity per bucket. The bucket resets when the next minute starts. The algorithm is cheap to implement — a single counter per identity — but allows a burst of `2 × limit` requests around the bucket boundary, since a client can spend its full quota in the last second of one minute and the first second of the next.

The **sliding window** algorithm smooths out the boundary effect by counting requests in the last N seconds rather than per fixed bucket. It is more accurate but more expensive: the server has to keep a list of timestamps per identity, or maintain a weighted average across two adjacent buckets.

The **token bucket** algorithm models the limit as a bucket that holds up to N tokens and refills at a steady rate (say, 10 tokens per second). Each request consumes one token; if the bucket is empty, the request is rejected. The bucket allows short bursts up to its capacity (a client can spend all 100 tokens at once after an idle period) while still enforcing the long-run average rate. This shape — burst tolerance plus a steady refill — fits most real client behaviour better than either fixed or sliding windows, which is why it is the algorithm most APIs default to.

| Algorithm | Burst behaviour | Cost | Use when |
|-----------|-----------------|------|----------|
| Fixed window | Allows 2× limit at the boundary | Single counter per identity | Cap is loose, simplicity matters |
| Sliding window | Smooth, no boundary spike | Timestamp list or weighted counter | Cap must be enforced strictly |
| Token bucket | Allows configurable bursts, enforces long-run rate | Two integers (tokens, last-refill timestamp) | Real client behaviour is bursty |

## A worked example

The chapter's running example is a quotes API — the same `Quote` domain (id, author, text, createdAt) used in the [REST API and DTOs exercise](/exercises/4-services-and-apis/1-rest-api-and-dtos/). The list endpoint paginates with cursors, and the whole API sits behind a token-bucket rate limiter.

```csharp
[HttpGet]
public async Task<ActionResult<PagedResponse<QuoteDto>>> GetQuotes(
    [FromQuery] string? cursor,
    [FromQuery] int limit = 20)
{
    limit = Math.Clamp(limit, 1, 100);
    var afterId = DecodeCursor(cursor);

    var quotes = await _db.Quotes
        .Where(q => afterId == null || q.Id > afterId)
        .OrderBy(q => q.Id)
        .Take(limit + 1)
        .ToListAsync();

    var hasMore = quotes.Count > limit;
    var page = quotes.Take(limit).Select(q => q.ToDto()).ToList();
    var nextCursor = hasMore ? EncodeCursor(page[^1].Id) : null;

    return Ok(new PagedResponse<QuoteDto>(page, new Pagination(nextCursor, limit)));
}
```

The action clamps the requested limit to a sensible ceiling (the client cannot ask for ten thousand rows), decodes the cursor, fetches one row more than the limit to detect whether more pages exist, and returns a `PagedResponse` envelope with `data` and `pagination` properties. The cursor is a base64-encoded id; the client treats it as opaque and passes it back unchanged. Sorting by `Id` ascending makes the cursor stable under inserts (a new quote always gets a higher id, so it appears on a future page rather than shifting an existing one).

The token-bucket policy lives in `Program.cs`:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddTokenBucketLimiter("quotes", config =>
    {
        config.TokenLimit = 100;
        config.TokensPerPeriod = 10;
        config.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        config.QueueLimit = 0;
        config.AutoReplenishment = true;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "1";
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded.");
    };
});

app.UseRateLimiter();
```

The bucket holds 100 tokens and refills at 10 tokens per second, so a client can issue a burst of 100 requests after an idle period and then settle into a steady 10 requests per second. When the bucket empties, the middleware short-circuits the pipeline, returns `429`, and writes a `Retry-After: 1` header so the client knows when to try again. The `[EnableRateLimiting("quotes")]` attribute on the controller (or on individual actions) opts the endpoint into the named policy.

The companion exercise [REST API and DTOs](/exercises/4-services-and-apis/1-rest-api-and-dtos/) does not yet wire pagination or rate limiting in, but the controller it produces is the natural place to bolt them on once the basic shape works.

## Summary

A list endpoint that returns the entire collection works in development and fails in production; pagination is the contract that keeps responses bounded. Offset, page, and cursor strategies all cap the response size but trade off differently on deep pages, stability under writes, and support for arbitrary jumps. Idempotency is a property of HTTP methods — `GET`, `PUT`, and `DELETE` are idempotent by definition, `POST` is not — and the `Idempotency-Key` header lets clients retry `POST` safely by giving the server a deduplication handle. Rate limiting is the backstop that protects shared resources from abuse and from accidental hot-loops, and the token-bucket algorithm fits most real client behaviour because it allows controlled bursts on top of a steady long-run rate. ASP.NET Core's rate-limiter middleware enforces the cap and returns `429 Too Many Requests` with a `Retry-After` header when a client crosses it. Together, these three mechanisms turn a working REST API into one that is also safe to expose.
