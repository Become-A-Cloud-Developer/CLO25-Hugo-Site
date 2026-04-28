+++
title = "The MVC Pattern"
program = "CLO"
cohort = "25"
courses = ["BCD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/3-application-development/3-the-mvc-pattern.html)

[Se presentationen på svenska](/presentations/course-book/3-application-development/3-the-mvc-pattern-swe.html)

---

A web application that handles user input, applies business rules, and renders output entangles three concerns that change for different reasons. Validation rules shift when policies change. Page layouts shift when designers iterate. Routing shifts when URLs are reorganized. Mixing those concerns in a single class produces code that becomes difficult to test, modify, or reason about. The MVC pattern resolves this by assigning each concern to a distinct role with a clearly defined contract between them.

This chapter covers the three roles of MVC, how routing matches a request URI to a controller action, how model binding turns request data into typed parameters, the action results that controllers return, the basics of Razor view syntax, and the request flow that ties everything together. The treatment focuses on ASP.NET Core MVC, the implementation used throughout the companion exercise [Presentation Layer](/exercises/10-webapp-development/1-presentation-layer/).

## The three roles of MVC

**MVC** (Model-View-Controller) is an architectural pattern that separates application concerns into three roles: the **model** holds data and business state, the **view** renders that state to the user, and the **controller** receives user input, coordinates the model and view, and decides what to return. Each role has a single reason to change and communicates with the others through narrow interfaces rather than shared internal state.

The split exists because the three concerns evolve at different rates and for different reasons. Input handling depends on URLs, HTTP methods, and form schemas. Business state depends on domain rules. Rendering depends on layout, styling, and presentation logic. Keeping the three apart means a designer can rework a view without touching validation logic, and a developer can change a routing rule without breaking the rendered HTML.

### Controller

A **controller** in ASP.NET Core MVC is a class whose public methods (called **actions**) handle incoming HTTP requests; an action returns an `IActionResult` describing what the framework should send back, such as a rendered view, JSON, or a redirect. Controllers in ASP.NET Core inherit from `Controller` and live by convention in a `Controllers/` folder. The class name ends in the suffix `Controller` — `HomeController`, `NewsletterController`, `AccountController` — and the framework strips that suffix when matching route values.

Each public method on a controller is a candidate action. The framework selects the action to invoke based on the route and the HTTP method. Methods can be decorated with attributes such as `[HttpGet]`, `[HttpPost]`, `[Route]`, or `[Authorize]` to constrain which requests they accept. An action method does the minimum needed to coordinate the response: it reads input from its parameters, calls into the service layer for business logic, and returns an action result. Controllers should not contain validation rules, persistence calls, or rendering logic; those belong to the service layer, the data layer, and views respectively.

### View

A **view** is a Razor template (`.cshtml`) that renders HTML by combining static markup with C# expressions; ASP.NET Core resolves view names to template files using conventions over the `Views/` folder. When a controller called `HomeController` returns `View("Index")`, the framework looks for `Views/Home/Index.cshtml`. The folder name matches the controller name (without the suffix), and the file name matches the view name. A shared `Views/Shared/` folder holds layouts and partials used across multiple controllers.

Views are not classes — they are templates compiled into rendering code at build or first-request time. A view receives a model object from the controller, exposes it through the `Model` property, and uses Razor expressions to interpolate values into HTML. Logic in views should stay limited to presentation concerns: looping over a collection of items, conditionally showing a section, formatting a value for display. Anything heavier — database queries, business rules, branching on user roles — belongs upstream.

In MVC, the **model** for a view is the typed data object passed to it. This usage is narrower than the broader notion of a domain model. Rich domain models — entities with behaviour, business invariants, and persistence concerns — live in the service layer covered in the [three-tier architecture chapter](/course-book/3-application-development/4-three-tier-architecture/). The model passed to a view is often a tailored shape, sometimes called a ViewModel, that contains exactly the fields the view needs to render and nothing more.

## Routing matches a URI to an action

**Routing** is the ASP.NET Core mechanism that matches an incoming request URI to a controller action, using either conventional templates configured at startup or attribute routes declared on actions. Routing is registered as middleware in `Program.cs` and runs early in the request pipeline. By the time an action executes, routing has already inspected the URI, found a matching template, and extracted route values from the path.

A conventional route template looks like `{controller=Home}/{action=Index}/{id?}`. Three placeholders define the structure: a `controller` segment, an `action` segment, and an optional `id` segment. The defaults `Home` and `Index` mean that a request to the root URL `/` resolves to `HomeController.Index()`. A request to `/Newsletter/Subscribe` resolves to `NewsletterController.Subscribe()`. A request to `/Products/Details/42` resolves to `ProductsController.Details(42)`, with `42` bound to the `id` parameter.

Attribute routing places the route directly on the controller or action with attributes such as `[Route("api/[controller]")]` or `[HttpGet("{id:int}")]`. Attribute routes are explicit, support route constraints, and are the preferred style for API controllers. Conventional routes are concise and suit traditional MVC applications where most routes follow a predictable controller-action pattern. The two styles can coexist in the same application.

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Figure 1: Default conventional route registered in `Program.cs`. The `=Home` and `=Index` syntax sets defaults for the segments, and the `?` makes `id` optional. With this single line, every controller action becomes reachable through a URL that mirrors its class and method name.

## Model binding turns request data into typed parameters

A controller action declares its inputs as ordinary C# parameters. **Model binding** is the ASP.NET Core process that converts incoming request data — route values, query strings, form fields, and JSON bodies — into the typed parameters of an action method. The framework inspects each parameter, looks for a matching value in the request, converts it to the parameter's declared type, and passes it in. The action body sees only typed data; the parsing of the raw request is handled before the method runs.

Model binding draws values from several sources in a defined order: route values come first, then query string parameters, then form fields, then the request body for content types like JSON. A parameter named `id` on `ProductsController.Details(int id)` matches the `{id}` route segment when present, falls back to a `?id=42` query string, and so on. For complex types — a class with several properties — binding walks the public settable properties and matches each one against the request by name.

Validation runs alongside binding. Properties decorated with attributes such as `[Required]`, `[StringLength]`, or `[EmailAddress]` are checked after the value is set, and the results accumulate in `ModelState`. The action body inspects `ModelState.IsValid` to decide whether to proceed or return the form to the user with error messages. The combination of typed parameters and declarative validation removes most of the boilerplate that would otherwise appear at the top of every action.

## Action results describe the response

An action method returns an `IActionResult` — an interface that represents what the framework should send back. The framework inspects the returned object and translates it into an HTTP response. Several concrete result types cover the common cases.

| Result type | Returned by helper | What it produces |
|-------------|-------------------|------------------|
| `ViewResult` | `View()` or `View(model)` | Renders a Razor view to HTML |
| `RedirectToActionResult` | `RedirectToAction("Index")` | Sends a 302 redirect to another action |
| `JsonResult` | `Json(data)` | Serializes an object to JSON |
| `NotFoundResult` | `NotFound()` | Returns HTTP 404 |
| `BadRequestResult` | `BadRequest()` | Returns HTTP 400 |
| `ContentResult` | `Content(text)` | Returns plain text or arbitrary content |

The choice of action result expresses the controller's intent. Returning `View()` says "render the page that matches this action." Returning `RedirectToAction` after a successful form post follows the post-redirect-get pattern, preventing the browser from re-submitting the form on refresh. Returning `Json` from an API action signals a machine-readable response. The framework handles serialization, status codes, and content-type headers based on the result type chosen.

## Razor view syntax

Razor is the templating syntax used by ASP.NET Core views. A `.cshtml` file is mostly HTML, with C# expressions introduced by the `@` symbol. A single expression like `@Model.Title` interpolates a value. A code block `@{ var count = Model.Items.Count; }` runs C# without producing output. Control flow uses standard C# keywords: `@if`, `@foreach`, `@switch`. The Razor parser distinguishes markup from code by tracking HTML tag boundaries, so most templates require no explicit signalling beyond the leading `@`.

Views typed against a specific model declare the type at the top with a `@model` directive: `@model Product`. From that point on, `Model` refers to the strongly-typed instance, and IntelliSense in the editor offers property completion. Layouts wrap views in shared markup using `_Layout.cshtml`, and partial views render reusable fragments through `@Html.Partial("_FormFields", Model)`. Tag helpers — attributes prefixed with `asp-` — bind form inputs to model properties and generate validation markup automatically.

## The request flow

A complete request flows through several stages, each isolated by an interface.

1. The browser sends an HTTP request to the server.
2. ASP.NET Core's hosting layer accepts the connection and constructs an `HttpContext`.
3. Middleware components run in order — authentication, static files, routing — each examining the request and either short-circuiting it or passing it on.
4. Routing matches the URI to a controller action and extracts route values.
5. Model binding populates the action's parameters from route values, query string, form, or body.
6. The framework instantiates the controller (using dependency injection for any constructor parameters) and invokes the action.
7. The action does its work and returns an `IActionResult`.
8. The framework executes the result, which may render a view, serialize JSON, or write a redirect header.
9. The response travels back through the middleware pipeline and out to the client.

Each stage has a clear responsibility, and each can be tested or replaced independently. Routing can be exercised without instantiating controllers. Controllers can be tested with mocked services. Views can be rendered against fabricated model objects.

## Worked example: HomeController and Index.cshtml

The default ASP.NET Core MVC project, scaffolded by `dotnet new mvc -n CloudSoft`, ships with a `HomeController` and a matching set of views. The controller is the simplest possible illustration of the pattern.

```csharp
using Microsoft.AspNetCore.Mvc;

namespace CloudSoft.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
```

Figure 2: `Controllers/HomeController.cs` from the scaffolded project. Each action is a parameterless method returning `View()` with no model. The framework resolves the view name from the action name — `Index` finds `Views/Home/Index.cshtml`, `Privacy` finds `Views/Home/Privacy.cshtml`.

```html
@{
    ViewData["Title"] = "Home Page";
}

<div class="text-center">
    <h1 class="display-4">Welcome</h1>
    <p>Learn about <a href="https://learn.microsoft.com/aspnet/core">building Web apps with ASP.NET Core</a>.</p>
</div>
```

Figure 3: `Views/Home/Index.cshtml`. The `@{ }` block runs C# to set a value in `ViewData`, a loosely-typed dictionary that the layout reads to populate the `<title>` tag. The rest is plain HTML that Razor passes through unchanged.

When a browser requests the root URL, the default route resolves the empty path to `controller=Home, action=Index`, the framework instantiates `HomeController`, calls `Index()`, receives a `ViewResult`, locates `Views/Home/Index.cshtml`, executes the Razor template against an empty model, and writes the resulting HTML to the response. The companion exercise [Presentation Layer](/exercises/10-webapp-development/1-presentation-layer/) extends this scaffold with strongly-typed models, form posts, and validation, exercising every part of the request flow described above.

## Why this pattern matters

The clean separation of controller, view, and model makes ASP.NET Core MVC applications testable and adaptable. A controller can be unit-tested by instantiating it with stub dependencies and asserting on the action result it returns. A view can be examined by rendering it against a hand-built model. A routing rule can be changed without touching either. When a feature evolves — a new field on a form, a different page layout, an alternate URL — the change tends to land in one place rather than rippling across the codebase.

The pattern also defines a clear seam for the next architectural layer. Controllers are thin; they depend on services for anything substantial. Those services form the **service layer** of the [three-tier architecture](/course-book/3-application-development/4-three-tier-architecture/), and they are supplied to controllers through [dependency injection](/course-book/3-application-development/6-dependency-injection/). The MVC pattern handles the boundary between HTTP and application code; the service layer handles the boundary between application code and business logic.

## Summary

The MVC pattern separates a web application into three roles: controllers handle input, views render output, and models carry data between them. ASP.NET Core MVC implements the pattern with controllers as classes whose actions return `IActionResult` values, views as Razor templates resolved by convention from a `Views/` folder, and models as the typed data passed from action to view. Routing matches an incoming URI to a specific action, and model binding converts route values, query strings, and form fields into typed parameters before the action runs. Action results — `ViewResult`, `RedirectToActionResult`, `JsonResult`, and the rest — describe the response declaratively, leaving the framework to produce the HTTP output. The complete request flow runs from middleware through routing, binding, action execution, and result rendering, with each stage isolated behind a clear interface. The result is a structure where presentation, input handling, and business state evolve independently, and where each can be tested without standing up the others.
