# Part IX — Mining Notes

Mined from studieguide ACD week 5 (v.19), BCD week 9 (v.13), and exercises 3-deployment/10-logging-and-monitoring.

## Chapter 1: The Three Pillars (Logs, Metrics, Traces)

**Key concepts:**
- Observability vs monitoring: observability is the ability to infer state from external outputs
- Three pillars: logs (narrative of what happened), metrics (quantitative samples, aggregated), traces (correlated flow across services)
- Logs alone answer "what?" but not "how fast?" or "how often?"
- Metrics alone answer aggregate questions but not the story of a single request
- Traces (distributed tracing, W3C Trace Context) tie logs and metrics together across service boundaries
- Sampling, retention, and cost tradeoffs differ per pillar
- Application Insights provides all three; Log Analytics stores logs and traces; Metrics are separate

**Borrowed references:**
- HTTP status codes, response time, latency (Part III Ch 1)

## Chapter 2: Structured Logging with ILogger

**Key concepts:**
- `ILogger<T>` is the canonical ASP.NET Core logging abstraction
- Generic registration: hosting model auto-registers `ILogger<>` against `Logger<>` in DI
- Message templates preserve structure: `"... {Field} ..."` vs string interpolation `$"... {field}..."`
- Placeholders are first-class fields in structured output, not dissolved into strings
- Log levels: Trace, Debug, Information, Warning, Error, Critical (six levels, ordered by severity)
- Per-category filtering: `appsettings.json` `Logging:LogLevel` controls which categories emit which levels
- Category matching is hierarchical (exact → prefix → prefix → Default)
- `ILogger.BeginScope(new Dictionary<string, object> { ["Key"] = value })` attaches fields to *every* log call inside the scope
- Console formatter vs fields: formatter is presentation (simple text, JSON), fields exist regardless
- Default console formatter prints text for humans; JSON formatter preserves structure for machines
- `ASPNETCORE_ENVIRONMENT=Production` (default in Container Apps) does not load `appsettings.Development.json`
- Structured logging is a discipline (constant template + placeholders) not a framework feature

**Exercise outcomes:**
- HomeController injects ILogger<HomeController> and logs "Home page rendered for {HostName} build {BuildSha}"
- Warning fired when BUILD_SHA env var missing
- Category-level filtering working per appsettings.json
- JSON formatter visible in Development, plain text in Production
- Same structured fields surface in both `dotnet run` and `docker run` and Container Apps stdout

**Borrowed references:**
- Dependency injection (Part III Ch 6)
- ASP.NET Core, MVC (Part III Ch 2)

## Chapter 3: Application Insights and Telemetry

**Key concepts:**
- Application Insights is an APM (Application Performance Management) sink
- Two modes: legacy (separate store), workspace-based (telemetry in Log Analytics workspace)
- Workspace-based mode is default in 2026: same store as container logs, KQL queries join across both
- Connection string (modern) vs instrumentation key (legacy): connection string includes regional endpoints
- Auto-instrumented: requests (with duration, status code, operation ID), dependencies (SQL, HTTP, Azure SDK calls), exceptions (type, message, stack trace)
- Trace telemetry = `ILogger` lines captured into App Insights
- Custom metrics: `TelemetryClient.GetMetric("name").TrackValue(value)` aggregates client-side, cheap at any volume
- Container Apps secret: inject via `secretref:` not literal string; env var sees the decrypted value at runtime
- Application Insights secrets scoped differently from plain env vars: secret references don't expose the value in `az containerapp show`
- Live Metrics: push-based, one-second latency, free (doesn't go through ingestion), limited retention
- Application Map: built from request and dependency streams, nodes show p95 latency and failure rate, edges show dependencies
- Failures blade: groups exceptions by type, shows stack trace and operation ID correlation
- SDK registration: `AddApplicationInsightsTelemetry()` in Program.cs, reads connection string from `APPLICATIONINSIGHTS_CONNECTION_STRING` env var
- Sampling: .NET SDK ships at 100% by default; adaptive sampling kicks in at high volume (>5 items/sec)
- Custom telemetry: `TelemetryClient` injected, used for custom metrics via `GetMetric` (cheap) or `TrackEvent` with properties (expensive, per-occurrence)

**Exercise outcomes:**
- Application Insights component provisioned in workspace-based mode
- Connection string injected as Container Apps secret, referenced via `secretref:`
- Live Metrics shows real-time request rate, latency, and server health
- Application Map shows single node with request count and failure rate
- /Home/Boom endpoint throws exception, visible in Failures blade after ~1 min ingestion delay
- Custom metric "home-page-views" tracked and visible in Metrics blade

**Borrowed references:**
- Azure Container Apps (Part VIII Ch 7)
- Managed identity (Part V Ch 8)

## Chapter 4: Log Analytics and KQL

**Key concepts:**
- Log Analytics workspace: multi-tenant store, many services stream data into same workspace, each into a table
- Container Apps environment: auto-creates Log Analytics workspace on provisioning, streams stdout/stderr into it
- Two table schemas: legacy `ContainerAppConsoleLogs_CL` (custom log, `_s` suffixes for strings) vs modern `ContainerAppConsoleLogs` (managed table)
- Modern schema preferred; queries written for one schema fail on the other
- KQL (Kusto Query Language): pipeline language, top-to-bottom, each operator transforms table
- Common operators: `where` (filter), `project` (select columns), `summarize` (aggregate), `order by` (sort), `take` (limit), `has` (tokenized substring), `extract` (regex), `extend` (add computed column)
- `search ""` dangerous without downstream `summarize`, queries all tables expensively
- Ingestion delay: 1-3 minutes from stdout to queryable in KQL, Log Analytics batches into search index every few seconds to minute
- Query pattern: source table → filter by time window → filter by app name → filter by content → project columns → sort → take limit
- `ago(30m)` = now minus 30 minutes
- Correlation ID: request-scoped unique ID tying multiple log lines from one request together
- Legacy schema: fields rendered into plain-text message, re-extraction via regex (`extract` function), newer approach is JSON console formatter
- Workspace retention and cost: Basic SKU charged per GB ingested and per GB retained, default 30 days, configurable to 2 years
- Saved queries: user (account-scoped) or workspace (RBAC-gated, requires Contributor)
- Functions: KQL stored procedures encapsulating query patterns

**Exercise outcomes:**
- Workspace discovered via portal or `az monitor log-analytics workspace list`
- Table schema identified (legacy vs modern)
- Traffic generated, waited for ingestion (1-3 min)
- KQL baseline query: filter by app, by time window, by message content
- Correlation ID middleware adds per-request unique ID to logging scope
- Two-replica scale confirmed in single KQL query grouping by `ContainerGroupName_s`
- Saved query "home-page-traffic" created
- Scaled back to one replica

**Borrowed references:**
- HTTP status codes (Part III Ch 1)

## Chapter 5: Health Checks

**Key concepts:**
- Health check: endpoint reporting if application (and dependencies) are healthy
- `/healthz` convention (Kubernetes-compatible)
- Liveness probe: is the process running? Returns quickly, no dependencies checked
- Readiness probe: is the service ready to serve requests? Checks dependencies (DB, cache, downstream API)
- Dependency probe: are external dependencies reachable and responding?
- Health check result: Healthy, Degraded, Unhealthy
- ASP.NET Core HealthChecks middleware: `AddHealthChecks().AddCheck<TCheck>()`, `MapHealthChecks("/healthz")`
- Custom health check: implement `IHealthCheck`, dependency injection, async check logic
- Container orchestrators poll liveness regularly; readiness gates traffic; both inform autoscaling decisions
- Health endpoint not served by main application controllers; separate, simplified logic reduces failure surface

**Sourced from:**
- Week 7 v.21 studieguide: "hälsokontroller, Google OAuth"
- Exercise 3-deployment/10-logging-and-monitoring context: Container App observable, need health signals

## Chapter 6: Alerts and SLOs

**Key concepts:**
- Alert rule: condition + threshold + notification channel
- Action group: where notifications go (email, webhook, SMS, integration with PagerDuty, Teams, Slack)
- Threshold: numeric boundary triggering alert (e.g., error rate > 1%, p95 latency > 500ms)
- SLI (Service Level Indicator): measured metric (e.g., "successful HTTP requests / total requests")
- SLO (Service Level Objective): target SLI (e.g., "99.9% availability over 30 days")
- Error budget: 1 - SLO, the acceptable failure rate (e.g., 99.9% → 0.1% error budget = ~43 min downtime/month)
- Log-based alerts: run KQL query, alert if result crosses threshold
- Metric-based alerts: more efficient than log-based, fewer ingestion and processing costs
- Alert fatigue: too many thresholds erode trust; choose meaningful SLOs and error budgets
- Alert rules can be workspace (shared) or user scoped

**Sourced from:**
- Week 5 context: observability enables proactive alerting
- Application Insights exercise "Going Deeper": alert on metrics, custom workbooks joining logs and metrics

**Borrowed references:**
- HTTP status codes (Part III Ch 1)

---

## Terminology Preview

Terms owned by this Part are detailed in part-9-glossary.md. Borrowed terms link to earlier Parts.
