+++
title = "Relational vs NoSQL Data Models"
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

## Relational vs NoSQL Data Models
Part IV — Data Access

---

## Two shapes of application data
- Ledgers and inventory want **strict structure** and atomic updates
- Catalogues and telemetry want **flexible structure** and high read throughput
- The data model shapes migrations, queries, and consistency

---

## How records are structured
- **Relational**: rows in typed tables, joined via foreign keys
- **Document**: self-describing JSON/BSON documents in collections
- Relational normalises facts; documents co-locate everything related

---

## Schema as a contract
- Relational schema is **enforced at write time** by the database
- Document schema lives in **application code**
- Rigid schemas reject bad data; flexible schemas defer the check

---

## The cost of migration
- Relational: versioned migration scripts in lockstep with code
- Document: collections hold **mixed shapes**; reads handle both
- The migration moved from the database into application logic

---

## Denormalization answers the join problem
- Document reads avoid joins by **duplicating data** across documents
- Trades write-time complexity for read-time simplicity
- Document boundary is the central modelling decision

---

## Eventual consistency
- CAP theorem forces a choice under partition
- **Eventual consistency**: replicas converge once writes stop
- Cosmos DB exposes Strong / Bounded / Session / Eventual as a knob

---

## Worked example: Cosmos DB MongoDB API
- Provision account, database, collection — **no schema declared**
- Driver inserts arbitrary BSON documents
- One-document read replaces a four-table join

---

## Decision framework
- Relational when invariants matter and data outlives code
- Document when shape evolves and scale beats joins
- Many systems use **both** — one model per bounded context

---

## Questions?
