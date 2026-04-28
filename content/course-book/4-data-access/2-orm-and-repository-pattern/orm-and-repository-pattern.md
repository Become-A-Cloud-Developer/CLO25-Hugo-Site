+++
title = "ORM and the Repository Pattern"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/4-data-access/2-orm-and-repository-pattern.html)

[Se presentationen på svenska](/presentations/course-book/4-data-access/2-orm-and-repository-pattern-swe.html)

---

A web application that mixes SQL strings or MongoDB filter expressions directly into its controllers and services becomes hard to change. The query syntax leaks into request handlers, the same lookup gets rewritten in three places with subtle differences, and every test of the business logic has to spin up a database. Two patterns address this: an **ORM** translates between database rows and in-memory objects so the code stops dealing in raw SQL, and the **repository pattern** wraps whichever data-access library is in use behind a single interface that the rest of the application calls. The two compose, and each carries trade-offs that decide when its abstraction cost is worth paying.

## Why raw data access becomes a liability

A handler that posts a newsletter to subscribers might begin life as a few lines of MongoDB driver code embedded in the controller — open the collection, build a filter, run the query, deserialize the result. The same shape repeats in three or four places: the admin dashboard listing newsletters, the public archive page, the notification job. Each copy carries the database client's vocabulary into a part of the code that has nothing to do with persistence.

That coupling has measurable costs. Renaming a field in the schema means searching every file for the field name. Replacing MongoDB with a relational store would require rewriting every handler that touches data. Tests that exercise business rules need a real database because there is no seam to substitute a fake. And the rules themselves — what counts as a published newsletter, which subscribers are active — get smeared across the codebase next to query syntax, making them hard to find when they need to change.

The standard response to this is to push data access into a dedicated layer. The [three-tier architecture](/course-book/3-application-development/4-three-tier-architecture/) chapter introduced that split: a presentation layer takes requests, a service layer enforces business rules, and a data layer talks to the [database](/course-book/2-infrastructure/storage/2-databases/). The two patterns this chapter covers are the standard tools for building that data layer.

## What an ORM does

An **ORM** (Object-Relational Mapper) is a library that translates between database rows and in-memory objects; the developer writes class definitions, and the ORM generates the SQL needed to load, save, and query those classes, often with change tracking that detects modified properties and writes only the deltas.

The translation runs in both directions. When a record is loaded, the ORM reads the row and populates the matching properties of an instance — column `published_at` becomes `Newsletter.PublishedAt`, foreign keys become navigation properties pointing at related entities. When the instance is saved, the ORM emits an `INSERT` or `UPDATE` statement that mirrors the changes. The application code never writes the SQL itself; it works with objects and method calls.

Three capabilities distinguish a full ORM from a simple mapper:

- **SQL generation** — the ORM produces the query text from a higher-level expression. Entity Framework Core in .NET, for example, takes a LINQ expression like `db.Newsletters.Where(n => n.PublishedAt != null).OrderByDescending(n => n.PublishedAt)` and emits parameterised SQL targeting the configured provider.
- **Change tracking** — once an entity is loaded, the ORM records its original values. On save, it compares current values against the snapshot and writes only the columns that changed. The application sets a property; the ORM works out whether to issue an `UPDATE` and what to put in the `SET` clause.
- **Identity management** — the ORM maintains one in-memory instance per primary key within a unit of work, so two queries that touch the same row return the same object. Updates therefore stay consistent across the active session.

ORMs targeting [document databases](/course-book/4-data-access/1-relational-vs-nosql/) — sometimes called ODMs (Object-Document Mappers) — apply the same idea to documents. The schema lives in class definitions, and the library serialises objects to BSON for MongoDB or to JSON for other document stores. The mechanics differ (no joins, no foreign keys), but the principle is identical: the application works with typed objects, and the library handles the wire format.

The cost is indirection. Generated SQL is sometimes less efficient than a hand-tuned query. Change tracking adds memory and CPU per loaded entity. Complex queries can be awkward to express through the ORM's expression API and may force a drop down to raw SQL anyway. None of these are reasons to avoid an ORM in a typical web application — they are reasons to know what the ORM is doing on the developer's behalf.

## The repository pattern

The **repository pattern** is a design pattern in which a single class (the repository) hides all data-access code behind an interface; service-layer code calls methods like `FindByIdAsync` or `InsertAsync` and the repository translates these into the underlying database client's calls, keeping query syntax out of business logic.

A repository exposes a small, deliberate vocabulary that matches the domain. Instead of `IMongoCollection<Newsletter>` with its `Find`, `Aggregate`, and `BulkWrite` methods, the rest of the code sees `INewsletterRepository` with methods like `GetPublishedAsync`, `FindBySlugAsync`, and `AddAsync`. The repository's job is to turn those domain-shaped calls into whatever the underlying client requires.

That seam buys three things. First, the service layer stops importing the database driver — it depends on an interface, not on `MongoDB.Driver` or `Microsoft.EntityFrameworkCore`. Second, swapping the storage technology becomes a contained change: a SQL-backed implementation of the same interface can drop in beside the MongoDB one. Third, unit tests can substitute a fake repository in milliseconds, so the service layer can be tested without a database.

A repository typically supports the four **CRUD** operations — **Create** (insert new records), **Read** (query existing records), **Update** (modify records), and **Delete** (remove records). Repository interfaces typically expose one method per operation, often with additional domain-specific finders layered on top.

### Generic versus domain-specific repositories

Two styles dominate. A *generic repository* is parameterised over the entity type — `IRepository<T>` — and exposes the CRUD operations that apply to any entity. A *domain-specific repository* declares an interface per aggregate — `INewsletterRepository`, `ISubscriberRepository` — and adds methods that only make sense for that type.

Generic repositories pay off when the application has many small entities with similar access patterns: load by id, save, list. Writing one `MongoRepository<T>` and registering it for each entity removes a lot of boilerplate. The cost is that domain-specific queries — "find all newsletters published in the last thirty days that have at least one open" — do not fit the generic interface. Forcing them through it tends to push query construction back into the service layer, which is exactly what the repository was meant to prevent.

Domain-specific repositories carry the opposite trade-off. They contain every query relevant to the domain object, so the service layer's calls read like business operations. They also cost more code: each new aggregate gets its own interface and implementation, and shared CRUD methods get re-declared per repository unless an inherited base class supplies them.

The common compromise is a generic base — `IRepository<T>` with the universal methods — that domain-specific interfaces extend with their own finders. The base implementation supplies CRUD; the derived repository adds whatever the service layer actually asks for.

| Style | Best when | Trade-off |
|-------|-----------|-----------|
| Generic `IRepository<T>` | Many small entities, mostly CRUD access | Domain-specific queries leak back into the service layer |
| Domain-specific `IXRepository` | Each aggregate has a distinct query surface | More interfaces and implementations to maintain |
| Generic base + domain-specific extension | Mix of universal CRUD and a few targeted queries | Slight extra setup; usually the right default |

## How ORM and repository compose

The two patterns operate at different layers, and a typical application uses both. The ORM (or ODM) sits at the bottom and handles the row-to-object translation. The repository sits on top of the ORM and presents a domain-shaped API to the service layer.

Without the repository, [services](/course-book/3-application-development/4-three-tier-architecture/) would talk to `DbContext` or `IMongoCollection<T>` directly. The query syntax would still leak across the codebase, just expressed in LINQ instead of SQL. With the repository in place, the ORM is an implementation detail of the data layer — visible inside `MongoRepository<T>` and `EfRepository<T>`, but not outside.

The reverse arrangement (a repository without an ORM) is also common. A MongoDB-backed repository typically calls the official driver directly rather than through an ODM; the driver already deserialises BSON into typed objects, which covers most of what an ODM would add. The repository wraps that driver behind the domain interface.

The composition matters because the patterns solve different problems. The ORM's job is to remove SQL or driver vocabulary from the data layer; the repository's job is to remove data-layer vocabulary from the service layer. Each one alone leaves the other concern unsolved.

### Worked example: an `INewsletterRepository`

The companion exercise [Data layer](/exercises/10-webapp-development/3-data-layer/) builds a repository over MongoDB for a newsletter application. The interface declares the operations the service layer needs:

```csharp
public interface INewsletterRepository
{
    Task<Newsletter?> FindByIdAsync(string id);
    Task<IReadOnlyList<Newsletter>> GetPublishedAsync();
    Task<Newsletter?> FindBySlugAsync(string slug);
    Task AddAsync(Newsletter newsletter);
    Task UpdateAsync(Newsletter newsletter);
    Task DeleteAsync(string id);
}
```

A generic base supplies CRUD against any MongoDB collection:

```csharp
public class MongoRepository<T> where T : class
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(IMongoDatabase database, string collectionName)
    {
        _collection = database.GetCollection<T>(collectionName);
    }

    public async Task<T?> FindByIdAsync(string id) =>
        await _collection.Find(Builders<T>.Filter.Eq("_id", ObjectId.Parse(id)))
                         .FirstOrDefaultAsync();

    public Task AddAsync(T entity) =>
        _collection.InsertOneAsync(entity);

    public Task UpdateAsync(string id, T entity) =>
        _collection.ReplaceOneAsync(Builders<T>.Filter.Eq("_id", ObjectId.Parse(id)), entity);

    public Task DeleteAsync(string id) =>
        _collection.DeleteOneAsync(Builders<T>.Filter.Eq("_id", ObjectId.Parse(id)));

    protected IMongoCollection<T> Collection => _collection;
}
```

The newsletter-specific repository extends the generic base with the domain finders:

```csharp
public class NewsletterRepository : MongoRepository<Newsletter>, INewsletterRepository
{
    public NewsletterRepository(IMongoDatabase database)
        : base(database, "newsletters") { }

    public async Task<IReadOnlyList<Newsletter>> GetPublishedAsync()
    {
        var filter = Builders<Newsletter>.Filter.Ne(n => n.PublishedAt, null);
        var sort = Builders<Newsletter>.Sort.Descending(n => n.PublishedAt);
        return await Collection.Find(filter).Sort(sort).ToListAsync();
    }

    public Task<Newsletter?> FindBySlugAsync(string slug) =>
        Collection.Find(Builders<Newsletter>.Filter.Eq(n => n.Slug, slug))
                  .FirstOrDefaultAsync();
}
```

Registration happens in `Program.cs` through [dependency injection](/course-book/3-application-development/6-dependency-injection/). The MongoDB client is registered as a singleton so the connection pool is shared across the application; the database handle is registered as a singleton too; the repository is registered behind its interface so the service layer can ask for `INewsletterRepository` and get the concrete implementation:

```csharp
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("Mongo")));

builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IMongoClient>().GetDatabase("newsletter_app"));

builder.Services.AddScoped<INewsletterRepository, NewsletterRepository>();
```

The service that publishes a newsletter now depends only on the interface:

```csharp
public class NewsletterService
{
    private readonly INewsletterRepository _repo;

    public NewsletterService(INewsletterRepository repo) => _repo = repo;

    public async Task PublishAsync(string id)
    {
        var newsletter = await _repo.FindByIdAsync(id)
            ?? throw new NotFoundException(id);
        newsletter.PublishedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(newsletter);
    }
}
```

The service contains no MongoDB types, no filter builders, no awareness of how `PublishedAt` becomes a BSON field. The repository owns those translations. Replacing MongoDB with a SQL store would mean writing a new `SqlNewsletterRepository` against the same interface and changing one DI registration; the service, the controller, and the tests stay the same.

## When the abstraction is worth its cost

Adding the repository layer is not free. Every entity gets an interface and an implementation. Every domain query gets named twice — once on the interface, once in the implementation. New queries that the service layer needs cannot be expressed inline; they have to be added to the repository first. For a small application with a handful of entities and straightforward queries, this overhead can outweigh the benefit.

A useful rule of thumb: the repository pattern earns its cost when at least one of these is true.

- **The application has more than one consumer of the same data.** A controller, a background job, and a CLI tool all reading newsletters benefit from a single shared `GetPublishedAsync` rather than three near-identical implementations.
- **The service layer needs to be unit-tested without a database.** An interface seam is the cheapest way to substitute a fake.
- **The storage technology might change, or already differs across environments.** A staging environment on PostgreSQL and a production environment on a managed cloud SQL service can share a service layer if the repository hides the difference.
- **The team values keeping query syntax out of business logic on principle.** This is a real and defensible reason; it is also the reason most often cited and the one most likely to lead to over-abstraction when the other reasons do not apply.

If none of those apply — a single small service, queries used in one place, no testing without the database — then the layer of indirection mostly adds noise. A direct call to `DbContext` or `IMongoCollection<T>` from the service is fine, and the repository can be introduced later when the costs of not having it start to show.

The same caution applies to over-generic repositories. A generic `IRepository<T>` with a fluent expression API, a specification pattern, and a custom query language tends to recreate the database driver one method at a time. At that point the abstraction is no longer hiding the driver; it is duplicating it. The interface is most valuable when it stays small and domain-shaped.

## Summary

Mixing database-driver calls into application code couples business logic to a specific storage technology, scatters query syntax across the codebase, and makes the rules hard to test in isolation. An **ORM** removes the row-level coupling by translating between database rows and in-memory objects, generating SQL from typed expressions and tracking changes so only modified columns are written. The **repository pattern** removes the next layer of coupling by wrapping the data-access library — the ORM, the MongoDB driver, or whatever else — behind a domain-shaped interface that the service layer calls. Together they give the data layer a single seam: the ORM controls how data crosses the database boundary, and the repository controls what the rest of the application sees. The patterns repay their cost when the same data has multiple consumers, when the service layer needs to be testable without a database, or when the storage technology may change. They become a liability when they are applied to a single small service or when the repository grows generic enough to recreate the driver underneath it. The companion exercise [Data layer](/exercises/10-webapp-development/3-data-layer/) implements `INewsletterRepository` over MongoDB and registers it through dependency injection, exercising both patterns end-to-end.
