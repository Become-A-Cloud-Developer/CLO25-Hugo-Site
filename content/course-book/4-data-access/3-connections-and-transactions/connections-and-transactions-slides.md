+++
title = "Connections, Pooling, and Transactions"
program = "CLO"
cohort = "25"
courses = ["BCD"]
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

## Connections, Pooling, and Transactions
Part IV — Data Access

---

## The cost of opening a connection
- TCP handshake, TLS handshake, protocol handshake, authentication
- 5–50 ms on local network, 100+ ms across regions
- Naive code opens one connection per request and burns latency on setup
- The fix is **not** fewer connections — it is **reusing** them

---

## The connection pool
- A **cache of open connections** kept inside the client library
- Idle connection available, return it; otherwise open a new one up to `MaxPoolSize`
- Configured through the **connection string** — `MaxPoolSize`, `MinPoolSize`, `ConnectionLifetime`
- Sized so `pool_size × replicas` stays under the database connection cap

---

## Where the pool lives
- Inside the **client object** in the application process — not a separate service
- Every replica has its own pool; the database sees the **sum** of all pools
- Constructing a new client per request defeats pooling completely
- Reuse the client; let the pool do its job

---

## Singleton client, Scoped repository
- Database clients (`MongoClient`, `BlobServiceClient`, Npgsql data source) are **thread-safe** and own the pool
- Register them as **Singleton** in DI — one instance per process
- Repositories built on top are registered as **Scoped** — one per request
- All scoped repositories share the same singleton client, and therefore the same pool

---

## Transactions
- A **transaction** is a unit of work the database treats atomically
- Either every operation commits, or any failure rolls everything back
- The application boundary for the **ACID** properties
- Keep transactions **short** — no HTTP calls, no user input inside them

---

## Isolation levels
- Controls what concurrent transactions see of each other's in-flight changes
- **Read Uncommitted** — fastest, allows dirty reads
- **Read Committed** — typical default, prevents dirty reads
- **Repeatable Read** — same row returns same value within a transaction
- **Serializable** — strongest, transactions behave as if run one at a time

---

## Optimistic vs pessimistic locking
- **Pessimistic** — lock the row before reading or writing; others wait
- **Optimistic** — read with a version, update only if version still matches
- Optimistic suits **low-contention** writes (profile edits)
- Pessimistic suits **high-contention** writes (last seat, last unit in stock)

---

## Practical rules
- **Reuse** client instances — Singleton in DI
- Keep **transactions short** and local to one database
- **Avoid distributed transactions** — prefer outbox pattern with local transactions
- Connection string lives in configuration, never in source

---

## Questions?
