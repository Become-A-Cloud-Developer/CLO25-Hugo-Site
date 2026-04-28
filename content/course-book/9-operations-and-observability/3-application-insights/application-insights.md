+++
title = "Application Insights and Telemetry"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 30
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/9-operations-and-observability/3-application-insights.html)

[Se presentationen på svenska](/presentations/course-book/9-operations-and-observability/3-application-insights-swe.html)

---

A controller that calls `_logger.LogInformation("Order {OrderId} shipped", id)` produces a log line — but a log line is only useful if something downstream collects it, indexes it, and lets a developer search across many of them at once. The previous chapter on [structured logging with ILogger](/course-book/9-operations-and-observability/2-structured-logging/) made the call site disciplined; the question that this chapter answers is where those calls go once the application is running on a managed platform, and how that destination grows from "a place that stores log lines" into a full picture of the system's behaviour. Application Insights is the answer that the .NET ecosystem reaches for first: a managed service that captures logs, request and dependency timing, exceptions, and custom telemetry from a deployed application without changes to most call sites. This chapter covers what the service actually is, how the SDK auto-instruments an [ASP.NET Core](/course-book/3-application-development/2-the-dotnet-platform/) app, the dashboards developers reach for during an incident, and the cost lever — sampling — that prevents a chatty service from generating an unmanageable bill.

## Why an APM destination

The framework's `ILogger` writes to whatever logging providers are registered. In development, that is the console. In a container running on [Azure Container Apps](/course-book/8-devops-and-delivery/7-azure-container-apps/), stdout flows into the Log Analytics workspace attached to the environment, which is enough to grep through container output with KQL. What is missing from that picture is everything that is not a log line: how long each HTTP request took, which downstream calls it made, which exceptions threw with what stack trace, and how all of those signals correlate per request. A pure log stream answers "what happened" one event at a time but leaves the developer to reconstruct the rest by hand.

**Application Insights** is a Microsoft Application Performance Management (APM) service that collects and analyzes telemetry from deployed applications, providing dashboards for Live Metrics, Application Map (dependency graph), Failures (exception analysis), Metrics, and Logs. It integrates with Log Analytics and can run in workspace-based mode so telemetry and container logs coexist in the same queryable store. In other words, it is the destination that turns the disciplined `ILogger` calls of the previous chapter into a navigable picture of a running system, and it adds three more pillars — request timing, dependency timing, and exception capture — that the application code does not have to author by hand.

Two operating modes exist for historical reasons. Legacy Application Insights kept telemetry in its own backing store, separate from the Log Analytics workspace where Container Apps streams stdout. Workspace-based mode points the component at an existing Log Analytics workspace, so Application Insights tables and `ContainerAppConsoleLogs` live side by side. Workspace-based mode is the default for new components and is the only mode worth using: the next chapter on KQL works against both kinds of data through one query language, and joining a request from the `requests` table with the container's stdout from `ContainerAppConsoleLogs` only works when both are in the same workspace.

## How the SDK captures telemetry without explicit calls

The reason Application Insights becomes useful with a single configuration line is that the .NET SDK auto-instruments the runtime. Adding `services.AddApplicationInsightsTelemetry(...)` to `Program.cs` registers a chain of telemetry initialisers and processors that hook into ASP.NET Core's request pipeline, the HTTP client factory, the SQL client, and the Azure SDK clients. From that point on, the SDK observes:

- **Requests**: every HTTP request the app handles, with its URL, method, duration, status code, and an operation ID that ties together everything that happened during that request.
- **Dependencies**: every outbound call the app makes — HTTP calls via `HttpClient`, SQL queries via `SqlClient`, Service Bus and Storage calls via the Azure SDK — annotated with target, duration, and success or failure.
- **Exceptions**: every unhandled exception that bubbles up to the framework, with its type, message, stack trace, and the operation ID of the request that caused it.
- **Traces**: every `ILogger` line at or above the configured level, captured into the `traces` table with the same operation ID as the request that produced it.

None of these require explicit `Track*` calls in application code. A controller method that throws an `InvalidOperationException` produces a `requests` row with a 500 status, an `exceptions` row with the stack trace, several `traces` rows for the framework's own log output, and possibly `dependencies` rows for any database queries the request executed before throwing — all sharing one operation ID. This automatic correlation is what makes the Failures blade and the Application Map possible without instrumented code.

The catch is that auto-instrumentation only captures what the SDK knows how to hook. A custom protocol over a raw socket, a worker process draining a queue without using one of the supported clients, or a mid-request side effect that does not flow through `HttpClient` will produce no dependency telemetry on its own. For those, the application has to opt in with an explicit telemetry-client call (covered later under Custom events and metrics).

## Configuration via connection string

The SDK is registered once and configured by environment variable, which keeps secrets out of source control and lets the same image run against different Application Insights components in different environments.

The connection string is the configuration format that replaced the legacy instrumentation key. It includes the instrumentation key, the regional ingestion endpoint, and the Live Metrics endpoint, all in one string. The SDK reads it from the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable by default, which means a Container App pointed at an Application Insights component needs only one secret reference to be fully wired up.

The wire-up sequence in the companion exercise looks like this. The Application Insights component is provisioned in workspace-based mode against the Log Analytics workspace that the Container Apps environment already owns. Its connection string is fetched from the resource's properties and stored as a Container Apps secret — `appinsights-cs` — rather than as a literal env var, so that `az containerapp show` does not surface the value in plaintext. The container app then declares an environment variable `APPLICATIONINSIGHTS_CONNECTION_STRING` whose value is `secretref:appinsights-cs`, which Container Apps decrypts and exposes to the running process at request time. From the SDK's perspective, the value is just an env var; from the platform's perspective, it never appears in any configuration export.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

builder.Services.AddControllersWithViews();

var app = builder.Build();
```

The single `AddApplicationInsightsTelemetry` call registers everything needed: the auto-instrumentation modules, the telemetry initialisers that stamp each item with the operation ID, the adaptive sampling processor, and the `TelemetryClient` that custom code can inject. Reading the connection string from configuration rather than relying on the default env-var lookup keeps the wiring explicit, which matters when the app needs to fail fast in development if the value is missing.

## The dashboards developers actually use

Application Insights ships with many blades, but four are reached for repeatedly during normal operation and during incidents.

**Live Metrics** is a real-time Application Insights dashboard showing current request rate, latency, server health (CPU, memory), and sample requests with one-to-two-second latency; it uses a separate push-based channel (not the regular ingestion pipeline) and is free because it bypasses storage, making it ideal for real-time monitoring during deployments or incident response. The page shows the last 60 seconds of activity, refreshing once per second. Because it bypasses ingestion, there is no 1–3 minute delay between the application emitting a signal and the dashboard rendering it, which is the property that makes it the dashboard to leave open while a deployment rolls out. If a new revision starts throwing exceptions, the failure rate spikes on Live Metrics within a couple of seconds; if the regular Failures blade is the only signal, the operator waits a minute or more before seeing anything.

The **Application Map** is an Application Insights visualization of system topology built from request and dependency telemetry; each node represents a logical service, and each edge represents a dependency (HTTP call, database query, message queue) annotated with p95 latency and failure rate. The map is generated automatically as the application makes calls to external services. For a single-service exercise, the map shows one node with its inbound request rate and any outbound dependencies — Storage, SQL, an HTTP API — drawn as edges. For a multi-service production system, the map becomes the answer to "which service is slow" or "which service is calling the database hardest", because the edges carry their own latency and failure-rate annotations.

**Failure analysis**, exposed by the Failures blade, refers to the part of the portal that groups exceptions by type and displays stack traces, affected operations, and sample instances. It allows developers to understand the distribution and impact of errors without querying raw logs, and links each failure back to the parent request via operation ID. The blade defaults to a time window — say the last 24 hours — and lists exception types ranked by occurrence, request URLs ranked by failure rate, and dependency targets ranked by failure rate. Clicking a row drills down to a sample failed operation, which contains the request, the exception, the stack trace, and every log line and dependency call that ran before the failure — the full reconstruction of one bad request, in one place.

The **Performance** blade does the same triage exercise for slow operations rather than failed ones. It ranks operations by p50, p95, or p99 latency, ranks dependencies by their contribution to slow operations, and links each row to a sample slow operation with the same drill-down semantics as Failures. Together with Failures, it covers the two failure modes that user-facing systems are most often debugged for: requests that errored, and requests that completed but were slow.

## Sampling as a cost lever

Telemetry is not free. Application Insights bills per gigabyte ingested into the workspace, and a chatty service that records a row per request, a row per dependency, a row per log line, and a row per exception can run into surprising bills at modest traffic. Sampling is the lever that bounds that cost without giving up statistical accuracy.

**Sampling** (in the telemetry sense) is the practice of sending a representative subset of telemetry items to the backend to reduce ingestion volume and cost. The Application Insights .NET SDK uses adaptive sampling, which starts at 100% (all items sent) and throttles back when the ingestion rate exceeds a threshold (e.g., 5 items per second), keeping aggregations statistically accurate through server-side reweighting. The SDK ships with adaptive sampling on by default, which means a low-traffic exercise sees every request, dependency, and exception, while a real production service automatically scales sampling down as load grows.

The thing that makes adaptive sampling tolerable rather than misleading is that it samples whole operations, not individual telemetry items. When a request is sampled out, every telemetry item produced during that request — the dependency calls, the log lines, the exceptions — is dropped together. The result is that a sampled request either has a complete record or no record at all; the Failures blade never shows an exception orphaned from its parent request. Aggregations like request count and average latency stay accurate because the SDK records the sampling rate per item and the backend reweights the results during query.

For most services, the SDK's default behaviour is the right starting point. Sampling becomes a knob worth tuning when the bill grows uncomfortable, when a specific failure type needs to be sampled at 100% even under load (configurable by exception type), or when an exporter pipeline is moving telemetry through OpenTelemetry rather than the native SDK and needs a hand-configured sampler.

## Custom events and metrics

Auto-instrumentation captures the runtime's view of the application; custom telemetry captures what the application alone knows. A "user added an item to a cart" event is not a log line in any meaningful sense — it is a business signal — and shoehorning it into `ILogger.LogInformation` is the wrong tool because the consumer is not a developer skimming logs but a dashboard counting cart-adds per hour.

A **telemetry client** is the programmatic interface (typically `TelemetryClient`) through which code sends custom telemetry (metrics, events, exceptions) to Application Insights; it is registered in the [dependency injection](/course-book/3-application-development/6-dependency-injection/) container and provides methods like `GetMetric()`, `TrackEvent()`, and `TrackException()` for application-level instrumentation beyond what the SDK auto-instruments. Two patterns dominate.

```csharp
public class HomeController : Controller
{
    private readonly TelemetryClient _telemetry;

    public HomeController(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }

    public IActionResult Index()
    {
        _telemetry.GetMetric("home-page-views").TrackValue(1);
        _telemetry.TrackEvent("HomePageViewed", new Dictionary<string, string>
        {
            ["UserAgent"] = Request.Headers.UserAgent.ToString()
        });
        return View();
    }
}
```

`GetMetric("home-page-views").TrackValue(1)` aggregates client-side: the SDK accumulates samples in memory and ships a pre-aggregated metric (count, sum, min, max, percentiles) once per minute. The cost is a single ingestion item per metric per minute regardless of traffic, which makes custom metrics cheap at any volume and is the right default for things being counted or measured many times per request. `TrackEvent("HomePageViewed", properties)` records one event per call, complete with arbitrary string properties, and is appropriate for low-frequency business signals (a checkout completed, a user signed up) where the per-occurrence detail matters.

Both kinds of custom telemetry inherit the operation ID of the surrounding request automatically, which means a custom event recorded inside a controller is correlated with the same `requests` row, the same `dependencies` rows, and the same `exceptions` row as everything else the request produced. The companion exercise [Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/) walks through the wire-up of `AddApplicationInsightsTelemetry`, the Container Apps secret reference, a deliberate `/Home/Boom` endpoint that throws to populate the Failures blade, and a `home-page-views` metric tracked from a controller — exactly the path described above.

## What Application Insights does not solve

Application Insights captures telemetry from one application's runtime. It does not, on its own, give a view across multiple applications unless they all point at the same component, and it does not replace the Log Analytics workspace for non-application data — the container's stdout, the platform's audit logs, the cluster's diagnostic telemetry. For systems composed of more than one service, the conventional pattern is one Application Insights component per environment, with every service in that environment pointing at it, so that the Application Map and the Failures blade aggregate across services rather than presenting one disconnected picture per service.

The chapter on [Log Analytics and KQL](/course-book/9-operations-and-observability/4-log-analytics-and-kql/) covers how the same workspace serves both Application Insights and Container Apps stdout via KQL, and how queries can join the two for the kind of question that neither blade answers on its own.

## Summary

Application Insights is a managed APM service backed by a Log Analytics workspace; in workspace-based mode the application's telemetry and the platform's container logs coexist in one queryable store. The .NET SDK auto-instruments ASP.NET Core to capture requests, dependencies, exceptions, and traces with operation-ID correlation, and is wired up by a single `AddApplicationInsightsTelemetry` call in `Program.cs` that reads its connection string from `APPLICATIONINSIGHTS_CONNECTION_STRING`. On Container Apps, the connection string is injected as a `secretref:` rather than a literal value, keeping it out of any configuration export. The portal exposes Live Metrics for the last 60 seconds with one-second latency, the Application Map for the dependency graph with p95 latency and failure rate per edge, the Failures blade for stack-traced exceptions grouped by type with operation-ID drill-down, and the Performance blade for the same drill-down on slow operations. Adaptive sampling is the cost lever — on by default, samples whole operations rather than individual items, and reweights server-side so aggregations stay accurate. Custom events and metrics, sent through an injected `TelemetryClient`, capture business signals that auto-instrumentation cannot see; `GetMetric` aggregates client-side and is cheap at volume, while `TrackEvent` records one item per call and is reserved for low-frequency, per-occurrence signals.
