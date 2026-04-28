+++
title = "Dependency Injection"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 60
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/3-application-development/6-dependency-injection.html)

[Se presentationen på svenska](/presentations/course-book/3-application-development/6-dependency-injection-swe.html)

---

A controller that needs to send a newsletter cannot send the newsletter itself — it relies on a service to validate the subscriber, on a repository to persist the address, and on a logger to record the outcome. The naive way to obtain those collaborators is for the controller to construct them with `new` inside its own body. That choice locks the controller to one specific implementation, makes the class unusable in tests without spinning up real infrastructure, and silently spreads object-graph wiring through every layer. This chapter develops a different approach: declare the collaborators a class needs, and let a runtime container build the graph.

## The problem with constructing collaborators directly

A class that creates its own collaborators carries two responsibilities at once: doing its own work, and assembling the objects it depends on. The two responsibilities pull in opposite directions. The work changes when the business rules change; the assembly changes when the implementation choices change. Mixing them together violates the [separation of concerns](/course-book/3-application-development/4-three-tier-architecture/) principle that the [three-tier architecture](/course-book/3-application-development/4-three-tier-architecture/) depends on.

Consider a controller written this way:

```csharp
public class NewsletterController : Controller
{
    private readonly NewsletterService _service =
        new NewsletterService(new InMemorySubscriberRepository());

    public IActionResult Subscribe(string email) =>
        _service.Subscribe(email) ? Ok() : BadRequest();
}
```

The controller now knows the concrete service type, the concrete repository type, and the order in which to construct them. Replacing the in-memory repository with one that talks to a database means editing the controller. Writing a unit test that does not actually store data means editing the controller. The controller is no longer focused on routing requests — it has become an object factory as well.

## Inverting the dependency

The fix is to invert who controls construction. Rather than the class reaching out for its collaborators, the class declares what it needs and waits for them to be supplied. **Dependency injection** (DI) is a design pattern in which a class declares the services it needs through constructor parameters and a runtime container supplies the concrete implementations, decoupling the class from the lifecycle and choice of those services.

The same controller, written for DI, looks like this:

```csharp
public class NewsletterController : Controller
{
    private readonly INewsletterService _service;

    public NewsletterController(INewsletterService service)
    {
        _service = service;
    }

    public IActionResult Subscribe(string email) =>
        _service.Subscribe(email) ? Ok() : BadRequest();
}
```

The controller no longer says how its service is built. It states a requirement: it needs something that satisfies `INewsletterService`. Whether the supplied object is a `NewsletterService` backed by a real database, an `InMemoryNewsletterService` used during development, or a `FakeNewsletterService` constructed in a unit test is a decision made elsewhere. The controller continues to work as long as the contract is honoured.

This style is called **constructor injection** — the dependency-injection style in which a class declares its required services as constructor parameters; the container resolves and supplies them when it instantiates the class, making the dependencies explicit and immutable. The constructor is the only place a dependency enters, the field that holds it is `readonly`, and the rest of the class can assume the dependency is present and valid.

## The ASP.NET Core DI container

The runtime piece that makes this work is the **DI container**. ASP.NET Core ships with one built in. At application startup, the container is told which interfaces map to which implementations and how long each instance should live. At runtime, when the framework needs to instantiate a controller, it inspects the controller's constructor, looks up each parameter type in the container, builds those dependencies (recursively resolving their own dependencies), and invokes the constructor with the resulting graph. The application code never calls `new` for any of these classes.

Registration happens through **`IServiceCollection`** — the ASP.NET Core builder used at startup (in `Program.cs`) to register services with the dependency injection container; each call associates a service type with a concrete implementation and a lifetime. Inside the project template generated by `dotnet new mvc`, `IServiceCollection` is exposed as `builder.Services`.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ISubscriberRepository, InMemorySubscriberRepository>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

var app = builder.Build();
```

Three things are registered. The framework's MVC services come from the `AddControllersWithViews` extension. The application's repository is registered with the service type `ISubscriberRepository` mapped to the concrete `InMemorySubscriberRepository`. The application's service is registered with the service type `INewsletterService` mapped to the concrete `NewsletterService`. After `builder.Build()` runs, the container is sealed and ready to resolve graphs on demand.

When a request to `/Newsletter/Subscribe` arrives, the framework sees that the matched action lives on `NewsletterController`. It asks the container for a `NewsletterController`. The container reads the constructor parameter list — `INewsletterService` — and asks itself for one. The `NewsletterService` constructor in turn asks for an `ISubscriberRepository`, which the container supplies as an `InMemorySubscriberRepository`. The whole graph is built top-down without any explicit wiring code in the controller, the service, or the repository.

### Interface-based vs concrete-type registration

Both registration styles are valid. Interface-based registration — `AddScoped<INewsletterService, NewsletterService>()` — is the common form for application code because it preserves the abstraction the layers are built on. Consumers depend on the interface, the container produces the implementation, and the implementation can be swapped without touching the consumers.

Concrete-type registration — `AddScoped<NewsletterService>()` — registers a single class with no interface in front of it. It still gives the container responsibility for construction and lifetime, but it offers no substitution point. Concrete registration suits internal helper classes that have only one implementation and no value as a test seam. The general guideline is to register against an interface whenever the class crosses a layer boundary or might be replaced by a fake during testing.

## Service lifetimes

Registration also declares how long an instance lives. A **service lifetime** controls how often the dependency injection container creates a new instance: **Singleton** lasts for the application lifetime, **Scoped** lasts for one request, and **Transient** creates a new instance every time the service is resolved.

The three lifetimes correspond to three registration methods on `IServiceCollection`:

| Method | Lifetime | When the container creates a new instance |
|--------|----------|-------------------------------------------|
| `AddSingleton<TService, TImpl>()` | Singleton | Once, the first time the service is resolved; reused for the entire application |
| `AddScoped<TService, TImpl>()` | Scoped | Once per HTTP request; reused for every resolution within that request |
| `AddTransient<TService, TImpl>()` | Transient | Every time the service is resolved, regardless of request |

### Singleton

A singleton instance is created once and shared by every component that depends on it for the lifetime of the process. Singletons fit services that hold expensive-to-build but immutable state — a parsed configuration, an in-memory cache that the application owns end-to-end, an HTTP client whose connection pool should be reused. Because the same instance is shared across concurrent requests, a singleton must be thread-safe; any mutable state inside it is a hazard.

Registering an in-memory repository as a singleton is appropriate when the in-memory store is the source of truth for the running process, and the underlying collection is built for concurrent access. The exercise version uses `ConcurrentDictionary` precisely so the repository can be shared safely.

### Scoped

A scoped instance is created once per request and discarded when the request ends. Within a single request, every component that resolves the service receives the same instance, which is useful when several layers need to participate in the same logical operation. Most application services and repositories are scoped because they often hold per-request state — for example, an Entity Framework `DbContext` that tracks entities loaded during the current operation.

Scoped is the safe default for application code that talks to per-request resources. The lifetime aligns with the natural unit of work in a web application: one request in, one response out, no shared state crossing the boundary.

### Transient

A transient instance is created every time the service is resolved, even within the same request. Transient suits lightweight stateless helpers — a small validator, a strategy object that is consulted once and thrown away. Transient is the most expensive lifetime in terms of allocations, so it should not be used for services that are heavy to construct or that intentionally cache work.

### Captive dependencies

The lifetimes interact in a way that creates a real failure mode. A singleton lives for the whole application; a scoped service lives for one request. If a singleton declares a constructor parameter of a scoped type, the container resolves the scoped service the first time the singleton is built — and that scoped instance is then held by the singleton for the rest of the application's life. The scoped service has effectively been promoted to a singleton, even though its implementation may not be safe to share across requests. This is called a _captive dependency_.

The reverse is fine: a scoped service may depend on a singleton. The danger is always the longer-lived consumer holding the shorter-lived service. A repository that maintains per-request state but is captured inside a singleton service will leak state from one user's request into another's. ASP.NET Core can detect some of these cases and throw at startup when validation is enabled, but the responsibility ultimately rests with the registration choices in `Program.cs`.

## A worked example: the newsletter service

The [service layer exercise](/exercises/10-webapp-development/2-service-layer/) builds a small newsletter feature against this pattern. The data layer holds subscriber email addresses; the service layer enforces uniqueness and basic validation; the presentation layer exposes a controller action.

The repository contract describes what a subscriber store can do, without saying how it stores anything:

```csharp
public interface ISubscriberRepository
{
    Task<bool> AddAsync(string email);
    Task<bool> ExistsAsync(string email);
}
```

The in-memory implementation backs the contract with a `ConcurrentDictionary` so that several concurrent requests can read and write safely:

```csharp
public class InMemorySubscriberRepository : ISubscriberRepository
{
    private readonly ConcurrentDictionary<string, byte> _subscribers =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> AddAsync(string email) =>
        Task.FromResult(_subscribers.TryAdd(email, 0));

    public Task<bool> ExistsAsync(string email) =>
        Task.FromResult(_subscribers.ContainsKey(email));
}
```

The service contract describes the business operation, without referring to where data lives:

```csharp
public interface INewsletterService
{
    Task<bool> SubscribeAsync(string email);
}
```

The implementation depends on the repository through its interface, validates the input, and delegates persistence:

```csharp
public class NewsletterService : INewsletterService
{
    private readonly ISubscriberRepository _repository;

    public NewsletterService(ISubscriberRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> SubscribeAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        if (await _repository.ExistsAsync(email)) return false;
        return await _repository.AddAsync(email);
    }
}
```

The controller accepts the service through its constructor. It does not know which repository the service uses, nor does the service know which controller called it:

```csharp
public class NewsletterController : Controller
{
    private readonly INewsletterService _service;

    public NewsletterController(INewsletterService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(string email) =>
        await _service.SubscribeAsync(email) ? Ok() : BadRequest();
}
```

Wiring all three together happens in one place, `Program.cs`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ISubscriberRepository, InMemorySubscriberRepository>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

var app = builder.Build();
```

The repository is registered as a singleton because the in-memory store *is* the application's data — a new instance every request would lose every subscriber. The service is registered as scoped because it carries no state of its own and pairs naturally with the per-request lifetime that real database-backed services will use later. The controller is not registered explicitly; ASP.NET Core's MVC integration registers controllers automatically and treats them as transient.

A swap to a database-backed repository later requires only one edit in `Program.cs`: replace `InMemorySubscriberRepository` with `SqlSubscriberRepository`, change the lifetime to scoped to match the database session, and the controller and service compile and run unchanged.

## Dependency injection enables unit testing

The same indirection that lets the registration line change without touching the controller also lets a test substitute a fake at construction time. A unit test for the controller does not need a database, an HTTP server, or even the DI container itself — it can hand-construct the controller with a fake service:

```csharp
public class FakeNewsletterService : INewsletterService
{
    public bool ReturnValue { get; set; }
    public string? CapturedEmail { get; private set; }

    public Task<bool> SubscribeAsync(string email)
    {
        CapturedEmail = email;
        return Task.FromResult(ReturnValue);
    }
}

[Fact]
public async Task Subscribe_returns_BadRequest_when_service_rejects()
{
    var fake = new FakeNewsletterService { ReturnValue = false };
    var controller = new NewsletterController(fake);

    var result = await controller.Subscribe("dup@example.com");

    Assert.IsType<BadRequestResult>(result);
    Assert.Equal("dup@example.com", fake.CapturedEmail);
}
```

The test is fast, deterministic, and isolated. It exercises the controller's behaviour — translating a service result into an HTTP outcome — without exercising any real service logic, persistence, or framework plumbing. The same shape of test applies to the service: a fake repository in, the service under test, no infrastructure required. This testability is not an accidental benefit; it is the direct payoff for declaring dependencies through interfaces and constructor parameters.

## Summary

Dependency injection moves construction out of the consumer and into a runtime container. A class declares the services it needs as constructor parameters; the ASP.NET Core DI container reads those parameters, resolves each one against its registrations, and supplies the graph. Registration happens once at startup through `IServiceCollection` calls in `Program.cs`, and each call binds a service type — usually an interface — to a concrete implementation and a lifetime. The three lifetimes are Singleton (one instance for the whole application), Scoped (one instance per request), and Transient (a new instance every time the service is resolved); registering a shorter-lived service inside a longer-lived consumer captures it and produces a captive-dependency bug. Constructor injection makes dependencies explicit and immutable, decouples each layer from the implementation choices of the layers beneath it, and turns unit testing into a matter of passing a fake into the constructor. The newsletter exercise applies the pattern end-to-end: a repository interface, a service interface, a single registration block, and a controller that knows nothing about how its collaborators are built.
