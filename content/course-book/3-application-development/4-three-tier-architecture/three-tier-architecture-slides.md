+++
title = "Three-Tier Architecture"
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

## Three-Tier Architecture
Part III — Application Development

---

## Why Monolithic Controllers Break Down
- A controller mixes **HTTP handling**, business rules, and data access
- Three reasons to change collide in one file
- Tests need a real database to exercise validation
- Swapping storage forces edits across every controller
- Duplicate rules drift between features over time

---

## The Three Layers
- **Presentation layer** — controllers and views; HTTP in, HTTP out
- **Service layer** — business logic, validation, orchestration
- **Data layer** — repositories that hide the storage technology
- Each layer has one reason to change
- Folders signal the structure; **interfaces** enforce it

---

## Layer Diagram
[Diagram: Presentation -> Service -> Data, arrows downward only, interfaces drawn at each boundary]

---

## Dependency Direction
- References flow downward only — never upward
- Controller depends on `INewsletterService`, not the concrete class
- Service depends on `ISubscriberRepository`, not the concrete class
- Concrete types are wired in `Program.cs` via the DI container
- Each layer compiles against an **abstraction**

---

## Request Flow: Newsletter Subscribe
- Controller receives POST, hands email and name to `INewsletterService`
- Service validates, calls `ISubscriberRepository.ExistsAsync` and `AddAsync`
- Repository writes to the store and returns
- Service returns an `OperationResult`
- Controller picks a redirect or re-renders the form

---

## Worked Example
- `NewsletterController` — presentation layer, returns `IActionResult`
- `NewsletterService` — service layer, returns `OperationResult`
- `InMemorySubscriberRepository` — data layer, writes to `ConcurrentDictionary`
- Replace the repository with a MongoDB-backed class — controller and service untouched
- Companion exercise: `/exercises/10-webapp-development/2-service-layer/`

---

## The Trade-Off
- More files per feature (4–6 instead of 1)
- Reusable business rules across features
- Substitutable storage backends
- Unit tests that exercise one layer at a time
- Worth the cost once the application grows past a single feature

---

## Questions?
