+++
title = "Dependency Injection"
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

## Dependency Injection
Part III — Application Development

---

## The Problem
- Controllers that **`new` their collaborators** mix work with assembly
- One concrete choice is **hard-coded** into the consumer
- Tests cannot substitute fakes without rewriting the class
- Object-graph wiring leaks into every layer

---

## The Inversion
- A class **declares** what it needs through its constructor
- A runtime **container** supplies the implementations
- The consumer depends on an **interface**, not a concrete type
- Construction order moves to **one place** at startup

---

## Constructor Injection
- Dependencies enter through **constructor parameters**
- Stored in **`readonly`** fields — immutable for the object's life
- Makes requirements **explicit** in the type signature
- Pairs naturally with **interface-based** registration

---

## Service Registration
- `IServiceCollection` is the **startup builder** in `Program.cs`
- `AddScoped<INewsletterService, NewsletterService>()` binds **interface to implementation**
- Concrete-type registration is valid for **internal** helpers
- Container is sealed once `builder.Build()` runs

---

## The Three Lifetimes
- **Singleton** — one instance for the whole application
- **Scoped** — one instance per HTTP request
- **Transient** — a new instance on every resolution
- Default for application services is usually **Scoped**

---

## Captive Dependencies
- A **Singleton** that holds a **Scoped** dependency captures it
- The shorter-lived service is **promoted** to the longer lifetime
- Per-request state then **leaks** between users
- Validate at startup; reverse direction (Scoped depends on Singleton) is safe

---

## Worked Example — Newsletter
- `INewsletterService` → `NewsletterService` registered **Scoped**
- `ISubscriberRepository` → `InMemorySubscriberRepository` registered **Singleton**
- `NewsletterController` receives **`INewsletterService`** through its constructor
- Swapping the repository edits **one line** in `Program.cs`

---

## Testability
- Hand-construct the controller with a **fake** service
- No DI container, no database, no HTTP server needed
- Tests are **fast, deterministic, isolated**
- The payoff for **interfaces + constructor injection**

---

## Questions?
