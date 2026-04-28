+++
title = "Three-Tier Architecture"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/3-application-development/4-three-tier-architecture.html)

[Se presentationen på svenska](/presentations/course-book/3-application-development/4-three-tier-architecture-swe.html)

---

A small ASP.NET Core application can put every concern into the controller: read the form, validate the input, check for duplicates, write to a database, render a confirmation. The code runs and the feature works. The trouble starts when the application grows. A second feature needs the same duplicate check. A third feature needs to call the same database, but with a different validation rule. The controller turns into a 500-line class that mixes HTTP handling, business rules, and data access, and any change risks breaking unrelated behaviour. Three-tier architecture addresses this by giving each kind of work its own home, with strict rules about which tier may call which.

## Why monolithic controllers break down

A controller's job is to translate between HTTP and the rest of the application. It receives a request, decides what should happen, and produces a response. When the controller also performs the business logic and reaches into the database directly, three different reasons to change the code collapse onto a single class. A change to the URL routing affects the same file as a change to a validation rule, which affects the same file as a change to the storage backend.

This violates a design principle known as **separation of concerns** — the design principle that each module of an application should have one reason to change; layers, classes, and methods are scoped so that unrelated responsibilities never live together. A class with three reasons to change attracts three streams of edits, three sets of bugs, and three groups of developers stepping on each other.

The practical symptoms appear quickly. Unit tests become hard to write because exercising the validation logic also requires a real database. Swapping an in-memory store for a cloud database forces edits to every controller. Two features that need the same business rule end up with two slightly different copies of it, drifting apart over time.

A **three-tier architecture** organizes an application into a presentation layer (UI), a service layer (business logic), and a data layer (persistence), with each layer depending only on the layer beneath it through abstractions. The structure does not eliminate complexity — a finished feature still needs HTTP handling, validation, and storage — but it places each piece behind a clear boundary, so changes stay local and substitutions stay possible.

## The three layers and their responsibilities

Each layer has one job. The boundaries between layers are formed by interfaces, not by folder names alone. Folders signal the structure to a developer reading the project; interfaces enforce the structure at compile time.

### The presentation layer

The **presentation layer** is the part of an application responsible for accepting user input and rendering output; in an ASP.NET Core MVC app, controllers and views form this layer. Code in this layer handles HTTP-specific concerns: route matching, model binding from form fields, returning `IActionResult`, setting status codes, and rendering Razor views. It does not contain business rules and does not talk to a database.

A controller in this layer reads the incoming request, hands the work to a service through a service interface, and translates the service's result back into an HTTP response. If the service reports success, the controller picks the appropriate view or redirect. If the service reports a validation failure, the controller returns the form with error messages. The controller does not know how the work is performed, only that it was requested and what the outcome was.

### The service layer

The **service layer** is the part of an application that contains the business logic — validation, workflow, orchestration — and exposes operations to the presentation layer through service interfaces. Code in this layer enforces rules: the email format must be valid, a subscriber must not exist twice, a publish action requires a draft to exist first. It coordinates one or more data-layer calls into a single operation that means something to the business.

The service layer does not know it is being called from HTTP. It receives plain objects (or primitive parameters) and returns plain objects (or a result type that signals success or failure). The same service class could be invoked from a background worker, a console tool, or a test, with no change to its code. This independence is what makes business logic reusable.

### The data layer

The **data layer** is the part of an application responsible for persistent storage and retrieval, exposed to the service layer through repository abstractions; concrete implementations may use a database, object storage, or an in-memory collection. Code in this layer translates between the application's domain types and the storage technology. A repository class hides whether the store is MongoDB, SQL Server, Azure Blob Storage, or a `ConcurrentDictionary` held in memory for a development environment.

The data layer enforces a different kind of rule: how data is laid out, indexed, and queried. It does not enforce business rules. A repository's `AddAsync(subscriber)` accepts whatever the service hands it; checking whether the subscriber is a duplicate is the service layer's concern.

## Dependency direction and the role of interfaces

The three layers are stacked, and references flow in one direction only. The presentation layer references the service layer; the service layer references the data layer. The arrows never point upward. A repository must not call a service; a service must not return a `ViewResult`.

This rule exists for two reasons. The first is testability. A service can be tested with a fake repository that stores rows in a list. A controller can be tested with a fake service that returns a canned result. If references pointed upward, every test of a low-level class would drag in the entire stack above it. The second reason is substitution. Replacing the in-memory subscriber repository with a MongoDB-backed implementation changes one class. None of the services or controllers above it need to be touched.

Interfaces enforce the boundary. A controller does not declare a parameter of type `NewsletterService`; it declares a parameter of type `INewsletterService`. A service does not declare a field of type `MongoSubscriberRepository`; it declares a field of type `ISubscriberRepository`. The concrete classes are wired up once, in `Program.cs`, through the dependency injection container. Each layer compiles against an _abstraction_ — an interface that names the operations it depends on without committing to any particular implementation.

This is also why dependency injection appears so often in three-tier code. Constructor parameters typed as interfaces are the mechanism by which a class declares its needs without choosing its dependencies. For the purposes of this chapter, the relevant point is that interfaces in constructors are how the layer boundaries are enforced. Concrete substitution is configured at startup; the layers above never name the concrete type. (See [the dependency injection chapter](/course-book/3-application-development/6-dependency-injection/) for the registration mechanics.)

The MVC pattern lives entirely inside the presentation layer. Controller, view, and the view's model object all sit at the top tier, and the controller delegates downward to a service interface. The MVC pattern itself is documented in [/course-book/3-application-development/3-the-mvc-pattern/](/course-book/3-application-development/3-the-mvc-pattern/); three-tier architecture wraps that pattern in two further layers below it.

## How a request flows through the layers

A single user action travels through all three layers and back. Tracing one request makes the boundaries concrete.

### Worked example: subscribing to a newsletter

The companion [service layer exercise](/exercises/10-webapp-development/2-service-layer/) refactors a monolithic controller into the three-layer structure described here. A user submits a newsletter signup form. The controller receives the POST, hands the email and name to the service, and the service uses a repository to store the subscriber.

The controller belongs to the presentation layer:

```csharp
public class NewsletterController : Controller
{
    private readonly INewsletterService _newsletterService;

    public NewsletterController(INewsletterService newsletterService)
    {
        _newsletterService = newsletterService;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(SubscribeFormModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await _newsletterService.SubscribeAsync(form.Email, form.Name);

        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Confirmation));
        }

        ModelState.AddModelError(string.Empty, result.Error);
        return View(form);
    }
}
```

The controller does three things, all of them HTTP-specific: it checks the model state populated by ASP.NET Core's model binding, it calls a single service method, and it picks a response based on the outcome. There is no email-format check, no duplicate check, no database call.

The service belongs to the service layer:

```csharp
public class NewsletterService : INewsletterService
{
    private readonly ISubscriberRepository _subscribers;

    public NewsletterService(ISubscriberRepository subscribers)
    {
        _subscribers = subscribers;
    }

    public async Task<OperationResult> SubscribeAsync(string email, string name)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return OperationResult.Fail("Email is required.");
        }

        if (await _subscribers.ExistsAsync(email))
        {
            return OperationResult.Fail("This email is already subscribed.");
        }

        var subscriber = new Subscriber(email, name, DateTime.UtcNow);
        await _subscribers.AddAsync(subscriber);
        return OperationResult.Ok();
    }
}
```

The service enforces the business rules: presence of an email, no duplicates, recording when the subscription occurred. It returns an `OperationResult` rather than a view or a status code, because the service has no concept of HTTP. The same method could be called from a unit test or a background job and behave identically.

The repository belongs to the data layer:

```csharp
public class InMemorySubscriberRepository : ISubscriberRepository
{
    private readonly ConcurrentDictionary<string, Subscriber> _store =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> ExistsAsync(string email)
        => Task.FromResult(_store.ContainsKey(email));

    public Task AddAsync(Subscriber subscriber)
    {
        _store[subscriber.Email] = subscriber;
        return Task.CompletedTask;
    }
}
```

The repository handles storage and nothing else. It does not validate, it does not check for duplicates as a business rule (the `ContainsKey` check exists to prevent a key collision in the dictionary, not to enforce policy). Replacing this class with a MongoDB-backed `MongoSubscriberRepository` changes the storage technology with no edits to `NewsletterService` or `NewsletterController`, because both depend on `ISubscriberRepository` rather than the concrete class.

The full request flow is: ASP.NET Core routes the POST to `Subscribe`, model binding fills `SubscribeFormModel`, the controller calls `SubscribeAsync`, the service validates and consults the repository, the repository returns a result, the service returns an `OperationResult`, the controller picks a redirect or a re-rendered view. Each layer does its own job and trusts the layers below.

## The trade-off: more files, more reasoning power

Three-tier architecture is not free. A signup feature that lived in one controller method now spans a controller, a service interface, a service class, a repository interface, a repository class, a domain object, and a result type. A reader new to the codebase must follow constructor parameters across several files to trace one operation.

The trade-off only pays off as the application grows. For a single signup form with a single rule, the layered version has more code than the monolithic version. For ten features sharing a duplicate-checking rule, the layered version has dramatically less code, because the rule lives in one place. For a feature that needs to swap from in-memory storage to a cloud database, the layered version requires changes to one file, and the monolithic version requires changes to every controller that touched the store.

| Concern | Monolithic controller | Three-tier |
|--------|----------------------|------------|
| Files per feature | 1 | 4–6 |
| Reuse of business rules | Copy-paste | Service method called from anywhere |
| Substituting storage | Edit every controller | Replace one repository class |
| Unit testing the rules | Requires real database | Pass a fake repository |
| New developer's first read | Direct (one file) | Indirect (follow interfaces) |

The decision rule is empirical: separate layers when there is more than one consumer of the rules, more than one storage backend in play, or more than one engineer touching the same area. For a throwaway demo, a monolithic controller is fine. For a product expected to live for years and grow new features, the layered structure is the cheaper option once the trajectory is taken into account.

## Summary

Three-tier architecture organizes an application into a presentation layer, a service layer, and a data layer, with dependencies pointing only downward and layer boundaries enforced by interfaces. The presentation layer translates between HTTP and method calls; the service layer enforces business rules and orchestrates work; the data layer hides the storage technology behind repository abstractions. Each layer has one reason to change, which is the principle of separation of concerns applied at the architectural scale. A request passes through all three layers — controller to service to repository — and the result returns up the same chain. The structure introduces more files, but the cost is repaid by reusable rules, substitutable backends, and tests that can exercise one layer at a time. Subsequent chapters cover how configuration is layered into this structure and how the dependency injection container wires the concrete implementations together at startup.
