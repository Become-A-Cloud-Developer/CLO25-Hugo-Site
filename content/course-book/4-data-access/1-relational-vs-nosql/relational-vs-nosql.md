+++
title = "Relational vs NoSQL Data Models"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/4-data-access/1-relational-vs-nosql.html)

[Se presentationen på svenska](/presentations/course-book/4-data-access/1-relational-vs-nosql-swe.html)

---

Application data rarely arrives in one tidy shape. A banking ledger needs every cent accounted for and every transfer balanced. A product catalogue grows new attributes whenever the merchandising team launches a category. A telemetry feed writes millions of small records that are read back as time ranges. The choice of [database](/course-book/2-infrastructure/storage/2-databases/) model — how records are structured, queried, and kept consistent — determines how naturally the storage layer fits each of those workloads. The two dominant choices are the relational model and the document model that dominates the NoSQL family, and the trade-offs between them shape every downstream decision a data layer makes.

## How the two models structure records

The relational model splits each entity into a row inside a table. Every table has a fixed set of columns with declared types, and rows in different tables connect to each other through foreign-key columns whose values must reference an existing primary key. A purchase, for example, lives across an `orders` row, several `order_items` rows, a `customers` row, and a `products` row, stitched together with `customer_id` and `product_id` foreign keys. The structure is normalised — each fact lives in exactly one place, and queries reassemble the full picture by joining tables.

The document model, the most common variant of the NoSQL family, takes the opposite approach. A **document database** stores each record as a self-describing document (typically JSON or BSON), grouped into collections; queries match documents by field values without the join machinery a relational database uses, and individual documents may have heterogeneous shapes. The same purchase becomes a single `Order` document containing the customer's name, an array of line items with embedded product names, and the shipping address — everything the typical read needs, in one place. Where the relational design optimises for storing each fact once, the document design optimises for retrieving everything related to one entity in one round trip.

### Schema as a contract

The structural difference is enforced through the **schema**. A schema is the formal description of the structure a data store enforces — table and column definitions in a relational database, or the set of fields a document is expected to carry in a NoSQL store; relational schemas are rigid and require migrations to change, while document schemas are flexible and can vary per document. In PostgreSQL, the schema is declared with `CREATE TABLE` and enforced on every write — a row that violates a `NOT NULL` or foreign-key constraint is rejected before it touches disk. In MongoDB or Azure Cosmos DB for MongoDB, the schema lives in application code: the database accepts whatever document the client sends, and any structural rules are enforced by the application or by an optional validation expression attached to the collection.

This shift moves a category of bugs. With a rigid schema, the database catches type mismatches, missing fields, and orphan references at write time. With a flexible schema, those mistakes only surface when a query expects a field a particular document never had — typically inside a code path that has already been deployed. The trade-off is real, and it is the load-bearing decision in this chapter.

## Schema rigidity and the cost of migration

Rigid schemas pay their cost upfront, in the form of *migrations*. Adding a column to a relational table is not a one-line code change — it is a coordinated operation that locks (or partially locks) the table, rewrites or backfills existing rows, and ships in lockstep with the application code that knows about the new column. Tooling such as Entity Framework migrations, Flyway, or Liquibase exists to make this disciplined: each schema change is a versioned script, applied in order, recorded in a migrations table the database itself owns.

Document databases avoid the lockstep deployment but do not eliminate the underlying problem. New code that writes a new field starts producing documents shaped differently from the older ones already in the collection. The collection now contains a mixture, and any read path has to handle both shapes — usually with a default value in code, or with a backfill job that rewrites the older documents. The migration has not vanished; it has moved into application code, where it is harder to track and easier to forget.

The deciding question is who pays the cost. A small team iterating quickly on a feature whose data shape is still being discovered benefits from the document model — the schema is allowed to drift while the design settles. A team operating a system whose invariants matter (a payments ledger, an inventory of physical goods, a regulatory reporting database) benefits from the relational model — the database refuses bad data, and the migration record is auditable.

## Denormalization as the answer to the join problem

A relational query joins tables to assemble a complete view. A document query reads one document. To make that single read sufficient, document designs use **denormalization** — the deliberate duplication of data across documents or tables so that a single read can satisfy a query without joining; common in document databases, it trades write complexity (keeping copies in sync) for query simplicity.

Consider the order example again. A relational design stores the product's name once, in the `products` table, and joins it in whenever an order is displayed. A document design copies the product name into every `Order` document at the time the order is placed. The read becomes one trip to the database; the write — when a product is later renamed — has to find every order that copied the old name and decide whether to update it. Sometimes the right answer is to update; sometimes (an order's snapshot of the product as it was sold) the right answer is to leave it. Denormalization is not a workaround; it is an explicit modelling choice that makes the trade-off visible.

The boundary of a document — what belongs inside it and what is referenced by id — is the central design decision in a document store. Embed too little and the application is reduced to fetching documents in a loop, recreating joins in client code with worse performance. Embed too much and a single document grows past practical size limits, becomes a write-contention hot spot, or forces unrelated updates to compete for the same record's lock.

## ACID, CAP, and eventual consistency

Relational databases historically prioritise [ACID](/course-book/2-infrastructure/storage/2-databases/) guarantees: a transaction either commits all of its changes or rolls back all of them, and committed changes are durable. These guarantees are why a bank transfer feels safe — the debit and credit succeed together, or neither happens.

Distributed databases must contend with the [CAP theorem](/course-book/2-infrastructure/storage/2-databases/), which states that under a network partition a system has to choose between consistency and availability. Many NoSQL stores choose availability: they keep accepting reads and writes on each side of a partition and reconcile afterwards. The model they expose is **eventual consistency** — the guarantee that all replicas of a piece of data will converge to the same value once writes stop, but at any single moment a read may return stale data; this is the consistency model many distributed NoSQL stores choose in exchange for availability under network partitions.

The application-visible effect is that a write the user just made may not appear in the next read served by a different replica. Cosmos DB exposes this as a configurable knob: the `Strong` consistency level serialises every read against the latest committed write at a single-region cost in latency, while `Eventual` accepts that some reads will be stale in exchange for the lowest latency and highest availability. Levels in between — `Bounded staleness`, `Session`, `Consistent prefix` — let an application name exactly how much staleness it is willing to tolerate.

Eventual consistency is acceptable for product catalogues, social feeds, telemetry, and analytics — workloads where a few seconds of staleness is invisible. It is not acceptable for ledgers, inventory of finite physical goods, or anywhere else two stale reads can produce a conflicting outcome that cannot be reconciled.

## Worked example: a Cosmos DB collection for the MongoDB API

The companion exercise [Portal Interface](/exercises/5-cloud-databases/1-portal-interface/) walks through provisioning an Azure Cosmos DB account configured for the MongoDB API. The act of creating the account never asks for a schema. Once the account exists, a database is created (a logical grouping), and inside it a collection — the document store's equivalent of a table. The collection demands one decision the relational equivalent does not: a *shard key*, the field Cosmos uses to distribute documents across partitions.

The follow-up exercise [Connecting from Code](/exercises/5-cloud-databases/4-connecting-from-code/) uses the official MongoDB driver to read and write documents from .NET. A typical insert looks like this:

```csharp
var client = new MongoClient(connectionString);
var database = client.GetDatabase("shop");
var orders = database.GetCollection<BsonDocument>("orders");

var order = new BsonDocument
{
    { "customer", new BsonDocument { { "name", "A. Lindberg" }, { "email", "a@example.com" } } },
    { "items", new BsonArray
        {
            new BsonDocument { { "sku", "BK-042" }, { "title", "Domain-Driven Design" }, { "qty", 1 }, { "price", 549 } }
        }
    },
    { "total", 549 },
    { "placedAt", DateTime.UtcNow }
};

await orders.InsertOneAsync(order);
```

The document carries the customer's name and email inline, and the line item carries the product title and price inline. Nothing in the database enforced that shape — the server accepted the document as written, and another insert against the same collection could carry a different set of fields. A read by customer email is a one-document fetch:

```csharp
var filter = Builders<BsonDocument>.Filter.Eq("customer.email", "a@example.com");
var match = await orders.Find(filter).FirstOrDefaultAsync();
```

The same query in a normalised relational design would join `orders`, `order_items`, `products`, and `customers`. The document version is one round trip and one server-side index lookup; the trade-off is that the product's title is a copy, and a later rename of the product has to be propagated explicitly if business rules require it.

## A decision framework

The two models are not interchangeable, and the choice shapes downstream decisions about migrations, query design, and consistency. The following table captures the trade-offs that decide most cases:

| Concern | Relational | Document |
|---------|-----------|----------|
| Schema enforcement | At write time, by the database | In application code or optional validators |
| Shape evolution | Versioned migrations, lockstep with code | Mixed-shape collections, handled in code |
| Joins across entities | First-class, declarative | Avoided through denormalization |
| Transactions across many entities | Standard (multi-row, multi-table) | Limited; per-document atomicity is the norm |
| Consistency under partition | Typically strong, at the cost of availability | Often eventual, configurable in Cosmos DB |
| Best-fit workloads | Ledgers, inventory, regulated data | Catalogues, content, telemetry, user profiles |
| Best-fit team posture | Invariants matter; data lives longer than code | Schema is still being discovered; iteration speed matters |

Several heuristics emerge from those trade-offs. Choose a relational database when the data has rich, stable relationships and the cost of an invalid row is high. Choose a document database when the dominant query reads one entity with all of its associated data, when the schema is genuinely evolving, or when horizontal scale and global distribution outweigh the convenience of joins. The cloud platforms make both choices managed services — Azure SQL Database and Azure Database for PostgreSQL on the relational side, Cosmos DB across document, key-value, and graph models on the NoSQL side — so the decision rests on data shape and consistency needs, not on operational capacity.

A frequent and reasonable answer is *both*. A retail system might keep its orders, products, and stock levels in PostgreSQL, where ACID transactions guarantee that a sold item is decremented from inventory atomically, and keep its product catalogue and search index in Cosmos DB, where flexible attributes and global reads matter more than transactional consistency. Picking a model per bounded context — rather than declaring one database the system's database — is usually the design that ages best.

## Summary

Relational and document data models structure records differently, and that difference cascades through schema management, query design, and consistency guarantees. Relational schemas are enforced at write time and evolved through versioned migrations; document schemas are enforced in application code and evolve as a mixed-shape collection. Document designs use denormalization to make a single read sufficient, trading write-time complexity for read-time simplicity. Distributed NoSQL stores often expose eventual consistency as the price of remaining available under network partitions, and Cosmos DB makes that knob explicit. The right model depends on the shape of the data and the cost of staleness — and a system serving more than one workload will frequently use both.
