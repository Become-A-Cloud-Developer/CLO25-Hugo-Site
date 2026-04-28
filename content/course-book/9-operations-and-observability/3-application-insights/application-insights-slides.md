+++
title = "Application Insights and Telemetry"
program = "CLO"
cohort = "25"
courses = ["ACD"]
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

## Application Insights and Telemetry
Part IX — Operations and Observability

---

## Why an APM destination
- `ILogger` writes to stdout — useful, but only one pillar
- A log stream answers **what** happened, not **how fast** or **how often**
- Request timing, dependency timing, and exceptions need a richer sink
- **Application Insights** is Azure's managed APM destination for .NET

---

## What Application Insights actually is
- A Microsoft **APM** service built on a **Log Analytics workspace**
- **Workspace-based mode** keeps app telemetry next to container stdout
- One queryable store for `requests`, `exceptions`, `traces`, `ContainerAppConsoleLogs`
- Legacy mode (separate store) is still possible, but never the right choice

---

## SDK auto-instrumentation
- **Requests** — URL, method, duration, status, operation ID per HTTP call
- **Dependencies** — `HttpClient`, SQL, Azure SDK calls timed automatically
- **Exceptions** — type, message, stack trace, correlated to parent request
- **Traces** — every `ILogger` line above the configured level

---

## Connection string and secret reference
- Modern format: a **connection string**, not a legacy instrumentation key
- SDK reads `APPLICATIONINSIGHTS_CONNECTION_STRING` from the environment
- On Container Apps: store as a secret, inject via `secretref:appinsights-cs`
- One line wires it up: `services.AddApplicationInsightsTelemetry(...)`

---

## The four dashboards developers reach for
- **Live Metrics** — last 60 seconds, ~1 s latency, free, bypasses ingestion
- **Application Map** — nodes per service, edges with p95 latency and failure rate
- **Failures** — exceptions grouped by type, drill-down via operation ID
- **Performance** — slow operations ranked by p50/p95/p99 latency

---

## Sampling as the cost lever
- Telemetry is billed per GB ingested — chatty services get expensive fast
- **Adaptive sampling** is on by default, throttles above ~5 items/second
- Samples **whole operations**, never orphaned exceptions
- Backend reweights server-side, so counts and averages stay accurate

---

## Custom events and metrics
- Inject `TelemetryClient` from DI, call `GetMetric` or `TrackEvent`
- `GetMetric("home-page-views").TrackValue(1)` — aggregates client-side, cheap
- `TrackEvent("HomePageViewed", props)` — one item per call, business signals
- Custom items inherit the surrounding request's **operation ID** automatically

---

## What it does not solve on its own
- One component per environment, all services pointing at it
- Non-app data (platform audit logs, infra metrics) lives elsewhere
- Cross-service correlation needs every service to share the component
- KQL across the workspace ties App Insights tables to container stdout

---

## Questions?
