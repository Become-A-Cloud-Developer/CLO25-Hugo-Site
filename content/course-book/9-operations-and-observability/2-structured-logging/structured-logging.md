+++
title = "Structured Logging with ILogger"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 20
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/9-operations-and-observability/2-structured-logging.html)

[Se presentationen på svenska](/presentations/course-book/9-operations-and-observability/2-structured-logging-swe.html)

---

A single application running on a developer laptop produces a few hundred log lines a day, and a human can read them. The same application running across several replicas in production produces hundreds of thousands per hour — most of them irrelevant most of the time, and the few that matter buried inside free-text strings that no machine can usefully filter. The discipline that closes this gap is structured logging: writing log calls so that the framework retains both the rendered text for humans and a set of named fields for machines. This chapter develops what structured logging means, how `ILogger<T>` and message templates implement it in [ASP.NET Core](/course-book/3-application-development/2-the-dotnet-platform/), when each log level applies, and how scopes and enrichment attach context without cluttering the call sites.

## From free-text strings to queryable data

A traditional log line is a string. Code such as `Console.WriteLine($"User {userId} signed in from {ip}")` produces output like `User 4129 signed in from 10.0.2.5`. The values are present, but they are concatenated into the message. To find every sign-in by user 4129, an operator must search for the substring `User 4129 ` and hope no other message uses the same prefix. To count sign-ins per IP address, the operator must extract the address with a regular expression, hoping the message format never changes.

**Structured logging** is the discipline of writing log lines with constant message templates containing named placeholders (e.g., `"User {UserId} signed in at {Timestamp}"`) rather than string interpolation; the placeholders preserve semantic fields that can be queried and aggregated downstream, transforming logs from searchable text into queryable data. The same call now emits two things in parallel: a rendered string for humans (`User 4129 signed in from 10.0.2.5`) and a structured record where `UserId = 4129` and `IpAddress = 10.0.2.5` are first-class fields. Downstream stores like Log Analytics index those fields as columns. A query for "all sign-ins by user 4129 in the last hour" becomes a filter on a column, not a substring search across a text blob.

The key insight is that [logs](/course-book/9-operations-and-observability/1-the-three-pillars/) are most valuable when their structure is preserved end-to-end. Every transformation that flattens the structure into prose loses information. The motivation for the rest of this chapter is to keep that structure intact from the call site to the query.

## How `ILogger<T>` works

ASP.NET Core builds on top of an abstraction called [Dependency injection](/course-book/3-application-development/6-dependency-injection/), and logging is one of the services it ships with by default. The host registers an open generic mapping from `ILogger<>` to the framework's `Logger<>` implementation. Any class that asks for `ILogger<SomeType>` in its constructor receives a logger that has already been configured with output sinks, filters, and formatters.

**`ILogger<T>`** is the generic logging abstraction in ASP.NET Core; it is registered by the host and injected into controllers, services, and middleware. The type parameter `T` (typically the class being logged) becomes the log category, enabling per-category filtering through configuration. `ILogger<T>` enforces message templates at compile time through overloads and is designed for structured logging with named fields.

The category matters because filtering happens per category. A `NewsletterController` logger has the category `Acme.Web.Controllers.NewsletterController`. A configuration block like `"Logging": { "LogLevel": { "Acme.Web.Controllers": "Debug" } }` raises the verbosity of every controller without affecting framework noise from `Microsoft.AspNetCore.*`. Category matching is hierarchical: the framework looks for an exact match first, then walks up the namespace prefixes, and finally falls back to `Default`.

### Message templates

A **message template** is a constant string containing named placeholders (e.g., `"Order {OrderId} shipped to {Address}"`) passed as the first argument to an `ILogger` method; the values corresponding to each placeholder are passed as trailing arguments. The template and values are kept separate by the logging framework, enabling structure preservation even when the final formatted message is rendered as text.

Two calls illustrate the difference. The first uses string interpolation:

```csharp
logger.LogInformation($"User {userId} signed in from {ipAddress}");
```

The second uses a message template:

```csharp
logger.LogInformation("User {UserId} signed in from {IpAddress}", userId, ipAddress);
```

Both produce the same human-readable string. The interpolation form, however, hands the framework a string that has already been collapsed: the values are no longer separable from the surrounding text. The template form hands the framework two things it can preserve independently — the constant template `User {UserId} signed in from {IpAddress}` (which becomes a stable identifier for this kind of event) and the values `{ UserId = ..., IpAddress = ... }` (which become structured fields).

Placeholder names are PascalCase by convention because they become field names in the output. The names matter; positions do not. The framework matches placeholders to arguments by left-to-right order, but the field name is taken from the template. A field consistently called `UserId` across the codebase aggregates cleanly in queries; one called `userId` in some calls and `user_id` in others does not.

A subtle but important consequence: never call `logger.LogInformation(message)` where `message` is itself an interpolated or concatenated string built at the call site. The first argument is a constant template, not a payload. Tools like `LoggerMessage.Define` and the source-generated `[LoggerMessage]` attribute help enforce this at compile time.

## Log levels

A **log level** is a severity classification assigned to a log message; ASP.NET Core defines six levels in order of severity: Trace, Debug, Information, Warning, Error, and Critical. Per-category filtering through `appsettings.json` controls which levels are emitted, allowing developers to tune the verbosity of each component independently without redeploying code.

Each level has a distinct use:

- **Trace** is for the most fine-grained signals, often values that change inside a tight loop. Trace output is normally disabled outside targeted debugging. It can include data that should not leave the developer's machine.
- **Debug** is for diagnostic detail useful while developing or troubleshooting a specific incident. Decision branches taken, payload sizes, intermediate identifiers. Off by default in production for cost and noise reasons.
- **Information** is the default level for the application's normal flow: a request started, a job completed, a user signed in. These are the events an operator wants to confirm "the system is doing the expected thing."
- **Warning** flags something unexpected but recoverable: a configuration value missing and falling back to a default, a retry that succeeded on the second attempt, a slow query that finished. The system continued; a human should know it happened.
- **Error** marks an operation that failed in a way the user noticed: a request that returned 500, a database write that threw, a background job that aborted. One log per failed operation, with the exception attached.
- **Critical** is for system-level failure that requires immediate intervention: data loss, security boundary violation, the process about to terminate. These should be rare and always actionable.

The discipline is to choose one level per call and stick to its meaning. A codebase that logs every loop iteration at Information drowns the operator in noise; one that logs every failure at Warning understates the severity and erodes trust in alerts. Production environments typically run with Information enabled for the application namespaces and Warning or higher for framework namespaces.

## Logging scopes

A request enters a controller, passes through several services, queries a database, and writes a response. Each layer logs something. Without correlation, the operator sees a flat sequence of unrelated lines and cannot tell which lines belong to which request. The fix is to attach a per-request identifier to every line emitted while that request is being processed.

A **logging scope** is a context established with `ILogger.BeginScope(...)` that attaches key-value pairs to every log call within that context; scopes are composable (nested) and commonly used to attach a correlation ID to all log lines from a single HTTP request or background job without changing each individual log call.

The pattern is a `using` block:

```csharp
using (logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = correlationId,
    ["UserId"] = userId,
}))
{
    logger.LogInformation("Loading newsletter {NewsletterId}", id);
    var newsletter = await repository.GetAsync(id);
    logger.LogInformation("Newsletter {NewsletterId} loaded ({ItemCount} items)",
        id, newsletter.Items.Count);
}
```

Both `LogInformation` calls inside the `using` block emit not only their own templated fields (`NewsletterId`, `ItemCount`) but also the scope fields (`CorrelationId`, `UserId`). The fields appear at the structured-data level — they are not concatenated into the message text. Scopes nest: a request-level scope might wrap a tenant-level scope, and both sets of fields attach to every inner log line.

Scopes only function when a logging provider opts in. The default console provider has scopes disabled by default; enabling them requires `IncludeScopes = true` in configuration. The JSON console formatter and most production sinks (Application Insights, Serilog, OpenTelemetry) include scope fields automatically.

## Enrichment

Some context belongs on every log line emitted by the application, regardless of which request triggered it: the machine name, the process ID, the application version, the deployment environment. Adding this on each call site is repetitive and error-prone.

**Log enrichment** is the practice of adding contextual fields to log records automatically — either globally (e.g., machine name, process ID via an initializer) or per-request (e.g., correlation ID via a scope). Enrichment keeps log calls focused on business logic while infrastructure details are injected by the framework.

Enrichment is configured once at host setup, typically in `Program.cs`. Application Insights, for example, exposes telemetry initializers that run for every emitted item and can stamp it with `cloud_RoleName`, `cloud_RoleInstance`, and the build SHA. Serilog and OpenTelemetry expose similar pipelines. The result is that every log line — without any change at the call site — carries the host context that an operator needs to filter by deployment, replica, or version.

A useful split is to think of enrichment in two layers. Global enrichment adds fields that never change for the lifetime of the process: machine name, build SHA, environment name. Per-request enrichment adds fields that change per request: correlation ID, authenticated user ID, tenant ID. Global enrichment is configured at startup; per-request enrichment is added by middleware that opens a scope at the start of the request and disposes it at the end.

## A worked example

Consider a controller for a newsletter service. Constructor injection takes an `ILogger<NewsletterController>`; the action wraps its work in a per-request scope and emits an Information line at the start and end of the operation.

```csharp
public class NewsletterController : Controller
{
    private readonly ILogger<NewsletterController> _logger;
    private readonly INewsletterRepository _repository;

    public NewsletterController(
        ILogger<NewsletterController> logger,
        INewsletterRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(string email)
    {
        var correlationId = HttpContext.TraceIdentifier;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
        }))
        {
            _logger.LogInformation(
                "Subscribe requested for {Email}", email);

            var result = await _repository.AddSubscriberAsync(email);

            if (result.AlreadyExisted)
            {
                _logger.LogWarning(
                    "Subscribe noop — {Email} already subscribed", email);
                return Ok();
            }

            _logger.LogInformation(
                "Subscribe completed — {Email} added as {SubscriberId}",
                email, result.SubscriberId);
            return Ok();
        }
    }
}
```

Three concerns are visible in the snippet. The category is bound to `NewsletterController` through the generic parameter, so configuration can dial logging for this controller independently from the rest of the app. The scope adds `CorrelationId` to every inner log line without each call having to mention it. The templates use PascalCase placeholders (`Email`, `SubscriberId`), which become structured fields in the output. A query that asks "how many Subscribe noops did user X cause yesterday" answers itself: filter the table by the constant template, group by `Email`, count rows.

A note on the `Email` field in this example: an email address is personally identifiable information. In a real implementation it would either be hashed before logging, replaced with the surrogate `SubscriberId`, or omitted entirely. The example shows the mechanism; the operational discipline follows.

## Operational practices

Structured logging unlocks queryable data, but it does not by itself impose discipline on what gets logged. Three practices matter most:

**No PII in logs.** Email addresses, phone numbers, full names, government identifiers, payment instruments — none of these belong in log streams. Logs are typically retained longer, replicated more widely, and exposed to more roles than the production database. Replace identifiers with surrogates (`SubscriberId` instead of `Email`), or hash sensitive values at the boundary so the log captures correlation without exposure.

**No secrets in logs.** API keys, connection strings, bearer tokens, and signed URLs all leak via logs more often than via code repositories. Treat logging as a sink that any operator with workspace read access can query, and assume any field written to it has been disclosed. Configuration values, request bodies, and exception messages all need scrubbing before they reach the logger.

**Sample at high volume.** A handler that emits a hundred log lines per request is fine in development; it costs significant ingestion budget once the application sees serious traffic. Drop chatty Debug calls in production. Use level filters per category to silence the framework's per-request lifecycle traces. For high-frequency events that still matter (e.g., per-cache-miss), consider sampling — log one in N — rather than every occurrence.

A fourth, smaller practice closes the loop: keep the message template constant. Every time the template changes, the structured event identity changes, and dashboards keyed on the old template stop counting. Add or rename fields, but resist the urge to "improve the wording" of a template that downstream queries already depend on.

The companion exercise [Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/) walks through wiring `ILogger<HomeController>` into a deployed ASP.NET Core MVC app, switching between the plain text and JSON console formatters, and watching the same structured fields appear in both `dotnet run` and Container Apps stdout.

## Summary

Structured logging treats each log call as two outputs at once: a rendered string for humans and a set of named fields for machines. ASP.NET Core implements this through `ILogger<T>`, which the host registers in dependency injection, parameterised by the logging category. Message templates with PascalCase placeholders preserve the field names downstream, so queries can filter and aggregate on them as columns rather than substring-search a text blob. Six log levels — Trace, Debug, Information, Warning, Error, Critical — partition severity, and `appsettings.json` controls per-category verbosity without code changes. Logging scopes attach correlation fields to every inner log line, and host-level enrichment stamps machine and deployment context onto every record. The operational discipline that makes this useful in production is the unglamorous half: no PII, no secrets, sample at high volume, and keep the templates stable so dashboards survive.
