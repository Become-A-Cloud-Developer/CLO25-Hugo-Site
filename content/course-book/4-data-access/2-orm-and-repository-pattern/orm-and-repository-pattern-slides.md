+++
title = "ORM and the Repository Pattern"
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

## ORM and the Repository Pattern
Part IV — Data Access

---

## Why raw data access hurts
- Database driver vocabulary leaks into controllers and services
- Same query gets rewritten in three places with subtle differences
- **Schema changes** require finding every embedded query
- Business rules sit next to filter syntax instead of standing on their own
- No seam to substitute a fake — every test needs a real database

---

## What an ORM does
- Translates between **database rows** and **in-memory objects**
- Generates SQL from typed expressions (LINQ, query builders)
- **Change tracking** writes only modified columns on save
- Identity map keeps one instance per primary key per session
- ODMs apply the same idea to document stores like MongoDB

---

## What the repository pattern does
- A single class hides all data-access code behind an **interface**
- Service layer calls `FindByIdAsync`, `AddAsync` — never the driver
- Storage technology becomes a contained, swappable choice
- Service layer becomes unit-testable with a fake repository
- Supports the four **CRUD** operations as its baseline

---

## Generic vs domain-specific
- **Generic** `IRepository<T>` — one implementation, lots of entities, mostly CRUD
- **Domain-specific** `INewsletterRepository` — queries that match the business
- Generic alone leaks domain queries back into the service layer
- Domain-specific alone duplicates CRUD across every aggregate
- Common compromise: generic base + domain-specific extension

---

## How they compose
- ORM sits at the bottom — row-to-object translation
- Repository sits on top — domain-shaped API to the service layer
- ORM removes driver vocabulary from the data layer
- Repository removes data-layer vocabulary from the service layer
- Each one alone leaves the other concern unsolved

---

## Worked example: NewsletterRepository
- `INewsletterRepository` declares `GetPublishedAsync`, `FindBySlugAsync`
- `MongoRepository<T>` base supplies CRUD via the MongoDB driver
- `NewsletterRepository` extends it with newsletter-specific finders
- Registered in `Program.cs` via **dependency injection**
- `NewsletterService` depends only on the interface — no MongoDB types

---

## When it pays for itself
- Multiple consumers reading the same data
- Service layer needs unit tests **without a database**
- Storage technology might change across environments
- Team values keeping query syntax out of business logic

---

## When it does not
- Single small service with queries used in one place
- No need to test the service without a database
- Generic interface grows to recreate the driver one method at a time
- The cost is real — only adopt it when it earns its keep

---

## Questions?
