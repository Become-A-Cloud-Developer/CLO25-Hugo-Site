+++
title = "DTOs vs Entities"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/6-services-and-apis/3-dtos-vs-entities.html)

[Se presentationen på svenska](/presentations/course-book/6-services-and-apis/3-dtos-vs-entities-swe.html)

---

An HTTP API exposes a contract that clients depend on, and a database that internal code depends on. The temptation is to use one class for both — a single `Quote` type that represents a row in storage and the JSON that flows over the wire. That choice feels economical for a week and expensive for a year. The shape of a stored row and the shape of a wire payload answer different questions, evolve at different rates, and break for different reasons. Keeping the two shapes separate from day one — with explicit types for each role and a small mapping function in between — is what lets each layer evolve on its own clock.

## Why entities make poor wire shapes

An **entity** is the in-memory class representing a row in the database (or a document in a document store); its shape is driven by persistence concerns — primary keys, navigation properties, audit columns — that the API client neither needs nor should see. Returning that entity directly from a controller action turns it into the public contract. Three forces pull against that arrangement.

### Over-exposure of internal fields

Entities accumulate fields the API has no business publishing. A `Quote` row in a real schema gains a `CreatedBy` user id, a `ModifiedAt` timestamp, a `RowVersion` concurrency token, perhaps a `IsDeleted` soft-delete flag. None of those are part of the contract a public client cares about, but a default JSON serializer prints every public property it sees. Adding the column ships the column. The first sign of trouble is usually a security review that finds an internal user id in a response body, weeks after the column was added for an unrelated migration.

The defensive workaround — sprinkling `[JsonIgnore]` attributes on the entity — works, but it inverts the safe default. A new property is exposed unless someone remembers to hide it. A separate response type inverts that default back: a new property is hidden unless someone explicitly adds it.

### Leaked persistence concerns

Persistence frameworks decorate entities with concerns that make sense to the database and noise to the client. Entity Framework navigation properties like `public Author Author { get; set; }` resolve through a relationship; serialised naively, they either trigger a lazy-load query mid-response or produce a deeply nested JSON tree the client never asked for. `Id` columns carry the database's choice of integer, GUID, or composite key — a wire shape might prefer a string, a slug, or no id at all on creation. A `RowVersion` byte array exists to support optimistic concurrency on writes, not to be base64-encoded into every read.

Coupling the wire format to these decisions means changing the database hurts clients. Switching from integer keys to GUIDs becomes a breaking API change. Adding an EF relationship adds an unbidden nested object. The wire shape should describe what the API exposes, not how the storage layer happens to keep it.

### Tight coupling between layers

When the entity is the wire shape, the API contract and the database schema become the same artifact. Schema migrations turn into client-visible changes. Renaming a column in storage requires a coordinated client release. Adding a column ships the column to every client whether they asked for it or not. Over time the team stops doing harmless internal refactors because every refactor is a public contract change.

The fix is to decouple the two layers explicitly with a type boundary. The entity evolves with persistence; the wire shape evolves with the contract; a small mapping function in between absorbs the difference.

## What a DTO is and how to design it

A **DTO** (Data Transfer Object) is a class shaped specifically for travelling over the wire between client and server; it carries only the fields the API contract exposes, and is decoupled from the persistence model so that database changes do not break clients and API changes do not require database migrations. The defining property of a DTO is that it has no behaviour and no persistence attributes — it is a flat record of fields, their types, and their validation rules.

Designing a DTO starts from the question "what does the client need to do its job?" rather than "what does the database happen to store?" That reframe leads to two practical decisions. First, the DTO's properties are a deliberate subset, not a copy. Second, the DTO is split by direction: one type for what the server accepts, another for what the server emits.

### Request DTOs for input validation

A *request DTO* describes what a client is allowed to send. The defining trait is that it omits every field the server controls. A client creating a quote does not choose its `Id` — the server assigns that on insert. The client does not choose `CreatedAt` either; the server stamps it when the row is written. Putting those fields in the request type would let a malicious or buggy client supply them, and the server would have to remember to throw the supplied values away.

Validation attributes attach naturally to the request DTO. Annotations like `[Required]`, `[StringLength]`, `[Range]`, and `[EmailAddress]` describe what a valid request looks like at the boundary. The `[ApiController]` attribute makes ASP.NET Core return a `400 Bad Request` automatically when a request fails validation, before any controller code runs. Keeping these annotations on the request DTO rather than the entity prevents two unrelated rules — request validation and domain invariants — from sharing the same attributes and changing for the same reasons. A request might cap an author's name at 100 characters because that is what the form allows; the entity might enforce a 200-character ceiling because that is what the column allows. Those are different rules even when the numbers happen to match.

### Response DTOs for selective exposure

A *response DTO* describes what the server returns. Its job is to expose exactly what the client needs and nothing else. Internal columns stay out by construction — there is no property to fill, so there is nothing to leak. A response DTO can also reshape the data: flatten a nested entity, rename a confusing column, format a `DateTimeOffset` as an ISO-8601 string with a known time zone, or compose multiple entities into a single resource view.

Splitting the response DTO from the request DTO has a second benefit: idempotent fields surface automatically. The response naturally carries server-assigned values like `Id` and `CreatedAt` because the server has them by the time it returns. The request naturally lacks them because the client cannot supply them. The compiler enforces the split.

## Mapping between entities and DTOs

A **mapper** is the code that converts between an entity and a DTO; it can be hand-written extension methods, an automated library such as AutoMapper, or constructor-style records that take an entity and emit a DTO. The mapper is small and almost always uninteresting, but choosing where it lives and how it is implemented shapes the experience of working in the codebase.

### Where mapping happens

Three locations are common, each with a different cost.

| Location | Strength | Trade-off |
|----------|----------|-----------|
| Inline in the controller action | Visible at the call site; trivial for one or two endpoints | Duplication grows with the resource count; controllers fill with shape-shuffling code |
| Dedicated mapper class or extension methods | One place to find the conversion; testable in isolation | Requires a small extra file; one more place to look |
| Library (AutoMapper, Mapster) | Zero hand-written code per type; convention-driven | Hidden behaviour; debugging a missing field means reading library config rather than code |

The companion exercise places the mapping in a private static method on the controller — `private static QuoteDto ToDto(Quote q)`. That is the lightest viable form for a single resource. A real codebase with five or ten resources usually graduates to one of the other two: a mapper class per aggregate, or extension methods grouped by entity. The library option is an optimisation that pays off only when the mapping count is high enough that hand-written code becomes a maintenance tax.

### Worked example

The exercise [REST Controllers, DTOs, and Swagger](/exercises/4-services-and-apis/1-rest-api-and-dtos/) defines the three types side by side. The entity captures the domain shape, including audit columns that may be added later. The two DTOs cover the two directions of traffic.

```csharp
// Entity — domain shape, may grow internal columns over time.
public class Quote
{
    public int Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    // Future internal columns: ModifiedBy, RowVersion, IsDeleted
}

// Request DTO — what the client is allowed to send.
public class CreateQuoteRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Author { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}

// Response DTO — what the server returns.
public class QuoteResponse
{
    public int Id { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
```

Three observations follow from the shapes. The request omits `Id` and `CreatedAt` because the server owns both — the controller calls `_store.Add(request.Author, request.Text)` and the store stamps the timestamp. The response carries every public field a client legitimately needs, and only those fields. The validation annotations live on the request type, so they describe the contract a client must satisfy without leaking onto the in-memory entity that downstream code uses after validation has already passed.

The mapper in the controller is one method:

```csharp
private static QuoteResponse ToDto(Quote q) => new()
{
    Id = q.Id,
    Author = q.Author,
    Text = q.Text,
    CreatedAt = q.CreatedAt,
};
```

When a future migration adds `ModifiedBy` to the entity, this method does not change and the response shape does not change. When the response gains a new computed field — say a `Slug` derived from the author and text — the change is local to the mapper and the DTO. Each layer evolves on its own clock.

## The cost of the split

The trade-off is real: more types means more typing. A small API with three resources gains three entity types, three or more request DTO types, three response DTO types, and a mapper per resource. A code generator or a library lowers the per-line cost, but the conceptual cost — two parallel hierarchies that have to stay roughly aligned — does not go away.

The cost is worth paying when any of the following holds. The API is public-facing or has clients outside the team's release control, so a change to the wire format requires a coordinated rollout. The persistence layer is non-trivial and likely to acquire internal columns. The validation rules differ from the domain invariants. The response shape differs from the storage shape — even slightly, like flattening one nested entity.

The cost is harder to justify when the API is a thin private endpoint inside a single deployable unit, the database schema is essentially the public contract, and the wire shape is provably identical to the storage shape forever. That description fits some internal admin tools and very little else. For most APIs that survive past their first release, the split pays for itself the first time the entity grows a column the wire should not see.

## Summary

Entities and DTOs answer different questions and should be kept as different types. The entity captures the domain shape that internal code reasons about, including persistence concerns like primary keys, navigation properties, and audit columns. The DTO captures the wire contract — exactly what the API exposes, with validation attributes describing what valid inputs look like. Splitting the DTO further into request and response types makes the asymmetry between client-supplied and server-controlled fields explicit at the type level, so the compiler enforces what discipline alone would not. The mapper between them is small, frequently boring, and the price of admission for letting the persistence layer and the API contract evolve independently. The cost — more types, more boilerplate — is the cost of a stable contract; it is paid back the first time storage changes without breaking clients, or the API contract changes without forcing a database migration.
