+++
title = "Connections, Pooling, and Transactions"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/4-data-access/3-connections-and-transactions.html)

[Se presentationen på svenska](/presentations/course-book/4-data-access/3-connections-and-transactions-swe.html)

---

A web application that talks to a database has to do two things well: open the conversation cheaply, and finish each unit of work without leaving the data in a half-changed state. Naive code gets neither right — every request opens a fresh connection, every multi-step write scatters partial updates when something fails, and concurrent users overwrite each other's changes silently. Three mechanisms solve those problems together: the connection string that locates the database, the connection pool that keeps the conversation cheap, and the transaction — with its isolation level and locking strategy — that keeps each unit of work coherent.

## The cost of opening a database connection

Opening a database connection is not a function call. It is a sequence of network and protocol steps that the application pays for every time it asks for a fresh connection.

A typical connection setup runs through:

1. A TCP three-way handshake to the database host (one round trip).
2. A TLS handshake if the link is encrypted (one or two more round trips).
3. A protocol-level handshake — for SQL Server, MongoDB, PostgreSQL, this includes capability negotiation and authentication.
4. Credential verification, often involving a password hash check or token validation against an identity provider.
5. Allocation of server-side resources — a session, a buffer, a worker thread.

On a low-latency local network this can take 5–50 milliseconds. Across regions or to a managed cloud database it can take 100 milliseconds or more. None of that work is the actual query — it is pure overhead before the first byte of useful data moves.

Code that opens a new connection per request scales badly under load. Each incoming HTTP request blocks for the handshake before it can read or write data, so request latency is dominated by connection setup rather than database work. A burst of concurrent requests floods the database with handshakes, exhausting authentication threads on the server long before query throughput becomes the bottleneck. The same connection that just finished a 2-millisecond `INSERT` is then closed and discarded — the next request rebuilds it from scratch.

The fix is not to open fewer connections. The application still needs a connection per concurrent operation. The fix is to keep the connections open and hand them out on demand.

## Connection pooling

A **connection pool** is a cache of open database connections that a client library reuses across requests, avoiding the cost of opening a new TCP connection and re-authenticating for each query; pool size is typically configured through the connection string. The pool turns connection setup into a once-per-process cost instead of a once-per-request cost.

The mechanics are straightforward. When the application asks the client library for a connection, the library checks the pool. If an idle connection is available, the library returns it immediately — no handshake, no authentication. If no connection is idle and the pool has not reached its maximum size, the library opens a new connection and adds it to the pool. If the pool is full, the request waits until another caller releases one. When the application finishes with the connection, the library does not close it; it returns it to the pool for the next caller.

### Where the pool lives

The pool lives inside the database client library, in the application process. SQL Server's ADO.NET driver maintains a pool keyed by connection string. The MongoDB driver keeps a pool inside each `MongoClient`. The Npgsql driver for PostgreSQL pools per process. The pool is not a separate service — it is a data structure inside the running application.

This placement matters for two reasons. First, every process has its own pool. Ten replicas of a web application each maintain their own pool of connections to the database; the database sees the sum of all pools. Second, the pool only helps if connections are returned to it. A client library that creates a new pool on every call (because the application keeps constructing new client objects) gets no benefit at all — every "pool" holds one connection and is then discarded.

### Configuring the pool

Pool behaviour is configured through the **connection string**, the configuration value a client library uses to connect to a database, encoding the host, port, database name, credentials, and optional parameters such as pool size or timeout. Common pool parameters include:

- `MaxPoolSize` (or `maxPoolSize`) — the upper bound on simultaneous connections from this process. Default values vary: 100 for SQL Server's ADO.NET, 100 for the MongoDB driver, around 100 for Npgsql.
- `MinPoolSize` — connections kept open even when idle, to absorb sudden bursts without paying handshake cost.
- `ConnectionLifetime` (or `maxLifetimeMS`) — the maximum time a single connection stays in the pool before being closed and replaced. This protects against connections that have silently gone stale (e.g. through a load balancer's idle timeout) and is the mechanism that lets a fleet of clients gradually pick up DNS changes after a database failover.
- `ConnectionTimeout` — how long a request waits for a free connection before throwing.

A SQL Server connection string with explicit pooling parameters might look like the following.

```text
Server=tcp:db.example.com,1433;Database=Newsletter;User Id=app;Password=...;Min Pool Size=5;Max Pool Size=50;Connection Lifetime=300;
```

A MongoDB connection string carries the same intent in URI form.

```text
mongodb://app:...@cluster0.mongodb.net/?retryWrites=true&maxPoolSize=50&minPoolSize=5&maxIdleTimeMS=60000
```

The numbers are not abstract. `MaxPoolSize` should be sized so the database can handle `pool_size × replica_count` simultaneous connections without exhausting its own connection limit. A managed database tier with a 200-connection cap and 8 application replicas leaves at most 25 pooled connections per replica before the database starts refusing connections. Sizing the pool above that limit causes failures under load that look like database outages but are really self-inflicted.

## Reusing the client across the application

The connection pool only delivers its benefit if the same client object is reused across the application. Constructing a fresh `MongoClient`, `SqlConnection` factory, or `BlobServiceClient` per request defeats pooling entirely — each new client builds its own pool, uses it once, and is discarded.

The standard way to ensure reuse in ASP.NET Core is to register the client with the [dependency injection container](/course-book/3-application-development/6-dependency-injection/) using the appropriate [lifetime](/course-book/3-application-development/6-dependency-injection/). For database clients that are thread-safe and hold a pool, the correct lifetime is [Singleton](/course-book/3-application-development/6-dependency-injection/) — one instance for the lifetime of the process, shared by every request. The pool inside that single instance is then reused across all incoming work.

### A worked example: registering MongoClient as a Singleton

The companion exercise [Data Layer](/exercises/10-webapp-development/3-data-layer/) wires a `MongoClient` into the dependency injection container in `Program.cs`. The connection string comes from configuration through [`IConfiguration`](/course-book/3-application-development/5-configuration-and-environments/), keeping credentials out of source control.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDb");
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<INewsletterRepository, MongoRepository<Newsletter>>();

var app = builder.Build();
```

The `IMongoClient` is registered as Singleton because `MongoClient` is documented as thread-safe and is intended to be created once per application. Internally it holds the connection pool, the topology monitor, and the server-selection state — all of which are expensive to set up and meant to be amortised. Repositories built on top of the client are registered as Scoped so each HTTP request gets its own repository instance, but every repository in the process shares the same underlying client and therefore the same pool. This pairing — Singleton client, Scoped repository — is the standard pattern for [document databases](/course-book/4-data-access/1-relational-vs-nosql/) accessed through the [repository pattern](/course-book/4-data-access/2-orm-and-repository-pattern/).

The same shape applies to other clients. `HttpClient` should be obtained from `IHttpClientFactory` rather than constructed per request. `BlobServiceClient` from `Azure.Storage.Blobs` is registered as Singleton. The Npgsql data source built with `NpgsqlDataSourceBuilder` is registered as Singleton and hands out pooled `NpgsqlConnection` objects on demand. The recurring rule: clients that own pools live for the process; objects derived from them live for the request.

## Transactions

A connection pool keeps individual operations cheap, but applications rarely run a single operation in isolation. Transferring a balance between two accounts is two updates. Placing an order is an `INSERT` into the order table plus a stock-level decrement. Cancelling a subscription touches three tables. If any one of those steps fails, the others must not be left applied — the system has to either complete the whole sequence or behave as if none of it happened.

A **transaction** is a unit of work that the database treats atomically: either every operation in the transaction succeeds and the changes commit, or any failure causes all changes to roll back as if they never happened. Transactions are the database's expression of the [ACID](/course-book/2-infrastructure/storage/2-databases/) properties at the application boundary — atomicity, consistency, isolation, and durability all manifest in how a transaction is begun, what it sees while it runs, and how it commits or rolls back.

In code, a transaction follows a fixed shape:

```csharp
using var transaction = connection.BeginTransaction();
try
{
    await DebitAsync(fromAccountId, amount, transaction);
    await CreditAsync(toAccountId, amount, transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

The `BeginTransaction` call marks the start. The `Commit` call makes every change durable in one step. The `Rollback` call (or letting the `using` block dispose without committing) discards every change made since `BeginTransaction`. The database guarantees that no other transaction will ever observe a half-applied state — the changes are either all there or none of them are.

Two practical rules apply. First, transactions should be short. A transaction holds locks (or version snapshots) for its entire duration; a long-running transaction blocks other work and increases the chance of deadlock. Reading data, calling an external HTTP service, and only then committing is an anti-pattern — the HTTP call dominates the transaction lifetime. Second, transactions should be local to a single database where possible. Distributed transactions that span two databases (or a database and a message broker) require a two-phase commit protocol, which is fragile, slow, and frequently the wrong abstraction. Patterns like the outbox pattern replace distributed transactions with local transactions plus eventually-consistent delivery, and are usually the better choice.

## Isolation levels

Concurrency is where transactions get interesting. The database has to decide what each in-flight transaction is allowed to see of the others' uncommitted changes. The dial that controls this is the **isolation level**, which governs what concurrent transactions are allowed to see of each other's in-flight changes; common levels — Read Uncommitted, Read Committed, Repeatable Read, Serializable — trade performance for stronger guarantees against lost-update, dirty-read, and phantom-read anomalies.

The four standard SQL isolation levels form a ladder:

| Isolation level | Dirty read | Non-repeatable read | Phantom read | Cost |
|-----------------|------------|---------------------|--------------|------|
| Read Uncommitted | possible | possible | possible | lowest |
| Read Committed | prevented | possible | possible | low |
| Repeatable Read | prevented | prevented | possible | medium |
| Serializable | prevented | prevented | prevented | highest |

A *dirty read* is reading data another transaction has written but not yet committed — and may still roll back. A *non-repeatable read* is re-reading a row inside the same transaction and getting a different value because another transaction committed an update in between. A *phantom read* is re-running a range query and finding new rows that weren't there the first time, again because another transaction committed an insert.

Most relational databases default to **Read Committed**, which prevents dirty reads but allows non-repeatable reads and phantoms. This default suits typical web workloads: each request is short, conflicts are rare, and the application can tolerate a small chance of seeing a value that has just changed. **Serializable** gives the strongest guarantee — every transaction behaves as if it ran by itself — at the cost of either heavy locking or frequent retries on serialization failures. It is the right choice for financial workflows where any anomaly is unacceptable, and the wrong choice for a high-traffic feed query that just needs an approximate snapshot.

## Optimistic and pessimistic locking

When two transactions touch the same row, the database has to decide what to do. The two strategies are named for the assumption they make about how often that happens.

**Pessimistic locking** prevents conflicts upfront by acquiring a database lock on a row before reading or modifying it; other transactions touching the same row block until the lock releases — useful when conflicts are common but expensive at scale. The lock is held for the duration of the transaction. Other readers and writers wait. The application gets a guaranteed-consistent view for the price of throughput: contended rows become a queue.

**Optimistic locking** detects conflicting updates by attaching a version field (or rowversion) to each record; an update succeeds only if the version still matches what was read, otherwise the application retries — useful when conflicts are rare and locking would hurt throughput. No locks are held between read and write. The transaction reads the row and its version, does its work, and on update writes a `WHERE id = ? AND version = ?` clause. If another transaction has since incremented the version, zero rows match and the update silently fails — the application catches the failure and retries.

The choice depends on the conflict rate. Editing a user's profile page is almost never contended; optimistic locking pays nothing in the common case and only adds work when a true conflict happens. Decrementing a global counter (last seat on a flight, last unit in stock) is highly contended; every transaction collides with every other, optimistic retries thrash, and pessimistic locking — or a different design entirely, like a queue or an atomic decrement — wins.

| Strategy | When to use | Trade-off |
|----------|-------------|-----------|
| Pessimistic | High contention, short critical sections | Blocks other transactions; risk of deadlock |
| Optimistic | Low contention, longer transactions | Retry logic in the application; can starve under heavy contention |

Some databases hide the choice. SQL Server with `READ COMMITTED SNAPSHOT` uses row versions for reads but locks for writes. PostgreSQL uses MVCC throughout, so readers never block writers and vice versa, and conflicts surface at commit time as serialization failures the application must retry. The vocabulary of optimistic versus pessimistic is still the right way to think about the problem even when the underlying mechanism is more nuanced.

## Practical advice

Three rules summarise the chapter:

- Reuse client instances. Register database clients as Singleton in the dependency injection container. Treat constructing a new client per request as a bug. The pool is the asset.
- Keep transactions short. A transaction should cover the smallest possible unit of work that must be atomic. No HTTP calls, no waiting on user input, no long-running computation. Begin, change, commit.
- Avoid distributed transactions when possible. A single-database transaction is straightforward; a transaction spanning two databases is a system-design problem in disguise. Prefer local transactions plus eventually-consistent delivery (the outbox pattern) over two-phase commit.

The companion exercise [Data Layer](/exercises/10-webapp-development/3-data-layer/) walks through registering a `MongoClient` as Singleton, configuring the connection string through `appsettings.json`, and observing that the same client serves every request without rebuilding its pool.

## Summary

Opening a database connection is expensive — a TCP, TLS, and protocol handshake plus authentication — so naive code that opens a connection per request collapses under load. The connection pool, hosted inside the client library and configured through the connection string, amortises that cost across requests by reusing open connections. The pool only helps if the client object itself is reused; that is why `MongoClient`, `SqlConnection`-factory, and similar clients are registered as Singleton in the dependency injection container while repositories layered on top are Scoped per request. Once connections are cheap, the next concern is making each unit of work coherent: a transaction wraps multiple operations into an atomic commit-or-rollback, the isolation level controls what concurrent transactions see of each other's in-flight changes (from Read Uncommitted to Serializable, trading throughput for stronger guarantees), and the locking strategy — optimistic with a version field for low-contention writes, pessimistic with row locks for high-contention ones — controls how conflicts are detected and resolved. Reuse client instances, keep transactions short, and avoid distributed transactions when possible.
