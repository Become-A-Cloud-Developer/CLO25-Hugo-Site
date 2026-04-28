# Part IX — Glossary

Terminology contract for the six chapters of Part IX — Operations and Observability.

## Terms owned by this Part

### Observability
- **Owner chapter**: `1-the-three-pillars`
- **Canonical definition**: **Observability** is the ability to infer the internal state of a system from the outputs it produces — logs, metrics, and traces. A system is observable when the questions asked of it can be answered without adding new instrumentation, relying instead on the signals the system already emits.
- **Used by chapters**: 1 (owner), 2, 3, 4, 5, 6

### Logs
- **Owner chapter**: `1-the-three-pillars`
- **Canonical definition**: **Logs** are timestamped, structured records of discrete events that occurred in the system; they form the narrative of what happened, in what order, with what context attached. Unlike metrics, logs capture the full detail of a single event (the error message, the exception stack, the request ID); unlike traces, logs exist within a single process and do not automatically cross service boundaries.
- **Used by chapters**: 1 (owner), 2, 3, 4

### Metrics
- **Owner chapter**: `1-the-three-pillars`
- **Canonical definition**: **Metrics** are numeric measurements sampled at regular intervals (e.g., every second or minute) that describe the state or behavior of a system; examples include CPU usage, request count per minute, and response time percentiles. Metrics are cheap to aggregate and store, making them ideal for answering "how is the system performing in aggregate" but poor at answering "what was the exact sequence of events."
- **Used by chapters**: 1 (owner), 3, 6

### Traces
- **Owner chapter**: `1-the-three-pillars`
- **Canonical definition**: **Traces** (in the distributed tracing sense) are causal chains of operations that flow through multiple services, tied together by a shared correlation ID (operation ID); they answer "how did a single user request flow through the system and where did time get spent." A trace includes one or more spans, each representing a unit of work (an HTTP request, a database query, a message consumption).
- **Used by chapters**: 1 (owner), 3

### Telemetry
- **Owner chapter**: `1-the-three-pillars`
- **Canonical definition**: **Telemetry** is the collective term for structured observability data — logs, metrics, traces, and events — that a system emits for external collection and analysis. Telemetry enables visibility into running applications without changing the code to add logging statements or metrics collection.
- **Used by chapters**: 1 (owner), 3

### Span
- **Owner chapter**: `1-the-three-pillars`
- **Canonical definition**: A **span** is a named unit of work within a distributed trace, representing a single operation (an HTTP request, a database query, a function call); it carries a start time, end time (duration), a status (success or error), and any events or attributes that occurred during its execution. Multiple spans from different services are linked by a shared trace ID and parent-span ID to form a complete trace.
- **Used by chapters**: 1 (owner)

### Structured logging
- **Owner chapter**: `2-structured-logging`
- **Canonical definition**: **Structured logging** is the discipline of writing log lines with constant message templates containing named placeholders (e.g., `"User {UserId} logged in at {Timestamp}"`) rather than string interpolation; the placeholders preserve semantic fields that can be queried and aggregated downstream, transforming logs from searchable text into queryable data.
- **Used by chapters**: 2 (owner), 3, 4

### ILogger<T>
- **Owner chapter**: `2-structured-logging`
- **Canonical definition**: **ILogger<T>** is the generic logging abstraction in ASP.NET Core; it is registered by the host and injected into controllers, services, and middleware. The type parameter `T` (typically the class being logged) becomes the log category, enabling per-category filtering through configuration. `ILogger<T>` enforces message templates at compile time through overloads and is designed for structured logging with named fields.
- **Used by chapters**: 2 (owner), 3, 4

### Log level
- **Owner chapter**: `2-structured-logging`
- **Canonical definition**: A **log level** is a severity classification assigned to a log message; ASP.NET Core defines six levels in order of severity: Trace, Debug, Information, Warning, Error, and Critical. Per-category filtering through `appsettings.json` controls which levels are emitted, allowing developers to tune the verbosity of each component independently without redeploying code.
- **Used by chapters**: 2 (owner), 4

### Message template
- **Owner chapter**: `2-structured-logging`
- **Canonical definition**: A **message template** is a constant string containing named placeholders (e.g., `"Order {OrderId} shipped to {Address}"`) passed as the first argument to an `ILogger` method; the values corresponding to each placeholder are passed as trailing arguments. The template and values are kept separate by the logging framework, enabling structure preservation even when the final formatted message is rendered as text.
- **Used by chapters**: 2 (owner), 4

### Logging scope
- **Owner chapter**: `2-structured-logging`
- **Canonical definition**: A **logging scope** is a context established with `ILogger.BeginScope(...)` that attaches key-value pairs to every log call within that context; scopes are composable (nested) and commonly used to attach a correlation ID to all log lines from a single HTTP request or background job without changing each individual log call.
- **Used by chapters**: 2 (owner), 3, 4

### Log enrichment
- **Owner chapter**: `2-structured-logging`
- **Canonical definition**: **Log enrichment** is the practice of adding contextual fields to log records automatically — either globally (e.g., machine name, process ID via an initializer) or per-request (e.g., correlation ID via a scope). Enrichment keeps log calls focused on business logic while infrastructure details are injected by the framework.
- **Used by chapters**: 2 (owner)

### Application Insights
- **Owner chapter**: `3-application-insights`
- **Canonical definition**: **Application Insights** is a Microsoft Application Performance Management (APM) service that collects and analyzes telemetry from deployed applications, providing dashboards for Live Metrics, Application Map (dependency graph), Failures (exception analysis), Metrics, and Logs. It integrates with Log Analytics and can run in workspace-based mode so telemetry and container logs coexist in the same queryable store.
- **Used by chapters**: 3 (owner), 4, 5, 6

### Telemetry client
- **Owner chapter**: `3-application-insights`
- **Canonical definition**: A **telemetry client** is the programmatic interface (typically `TelemetryClient`) through which code sends custom telemetry (metrics, events, exceptions) to Application Insights; it is registered in the dependency injection container and provides methods like `GetMetric()`, `TrackEvent()`, and `TrackException()` for application-level instrumentation beyond what the SDK auto-instruments.
- **Used by chapters**: 3 (owner)

### Sampling
- **Owner chapter**: `3-application-insights`
- **Canonical definition**: **Sampling** (in the telemetry sense) is the practice of sending a representative subset of telemetry items to the backend to reduce ingestion volume and cost. The Application Insights .NET SDK uses adaptive sampling, which starts at 100% (all items sent) and throttles back when the ingestion rate exceeds a threshold (e.g., 5 items per second), keeping aggregations statistically accurate through server-side reweighting.
- **Used by chapters**: 3 (owner)

### Live Metrics
- **Owner chapter**: `3-application-insights`
- **Canonical definition**: **Live Metrics** is a real-time Application Insights dashboard showing current request rate, latency, server health (CPU, memory), and sample requests with one-to-two-second latency; it uses a separate push-based channel (not the regular ingestion pipeline) and is free because it bypasses storage, making it ideal for real-time monitoring during deployments or incident response.
- **Used by chapters**: 3 (owner)

### Application Map
- **Owner chapter**: `3-application-insights`
- **Canonical definition**: The **Application Map** is an Application Insights visualization of system topology built from request and dependency telemetry; each node represents a logical service, and each edge represents a dependency (HTTP call, database query, message queue) annotated with p95 latency and failure rate. The map is generated automatically as the application makes calls to external services.
- **Used by chapters**: 3 (owner)

### Failure analysis
- **Owner chapter**: `3-application-insights`
- **Canonical definition**: **Failure analysis** (in Application Insights) refers to the Failures blade, which groups exceptions by type and displays stack traces, affected operations, and sample instances. It allows developers to understand the distribution and impact of errors without querying raw logs, and links each failure back to the parent request via operation ID.
- **Used by chapters**: 3 (owner)

### Log Analytics workspace
- **Owner chapter**: `4-log-analytics-and-kql`
- **Canonical definition**: A **Log Analytics workspace** is a multi-tenant storage container in Azure where multiple services (Container Apps, Application Insights, VMs, and others) send their telemetry; data is organized into tables, each scoped to a service type. The workspace provides a single point for retention policy, role-based access control, and KQL-based querying across all ingested data.
- **Used by chapters**: 4 (owner), 5, 6

### KQL (Kusto Query Language)
- **Owner chapter**: `4-log-analytics-and-kql`
- **Canonical definition**: **KQL** (Kusto Query Language) is a functional, case-insensitive query language used in Azure Data Explorer and Log Analytics; it uses a pipe (`|`) composition model where each operator transforms the table flowing through it, enabling users to filter, aggregate, and visualize telemetry data through expressive, readable queries that read like English.
- **Used by chapters**: 4 (owner), 5, 6

### Table
- **Owner chapter**: `4-log-analytics-and-kql`
- **Canonical definition**: A **table** (in Log Analytics) is a named collection of rows, each with the same schema, representing a single data source type (e.g., `ContainerAppConsoleLogs` for container stdout, `InsightsMetrics` for Application Insights metrics). Queries select one table as a source and apply operators to filter, project, and aggregate its rows.
- **Used by chapters**: 4 (owner)

### Query operator
- **Owner chapter**: `4-log-analytics-and-kql`
- **Canonical definition**: A **query operator** (in KQL) is a function that transforms a table flowing into it and outputs a transformed table; common operators include `where` (filters rows), `project` (selects columns), `summarize` (aggregates), `order by` (sorts), `take` (limits rows), and `extend` (adds computed columns). Operators are chained via the pipe symbol to compose complex queries.
- **Used by chapters**: 4 (owner)

### Time range
- **Owner chapter**: `4-log-analytics-and-kql`
- **Canonical definition**: A **time range** is the window of historical data a KQL query operates on; expressed in queries as conditions like `where TimeGenerated > ago(30m)`, it controls both the scope of analysis and the query performance. Shorter time ranges are cheaper and faster; longer ranges increase cost and latency.
- **Used by chapters**: 4 (owner)

### Retention
- **Owner chapter**: `4-log-analytics-and-kql`
- **Canonical definition**: **Retention** is the length of time data is kept in a Log Analytics workspace before automatic deletion; the default is 30 days, but can be configured from 7 days to 2 years. Longer retention increases storage cost and query latency; shorter retention reduces cost but limits historical analysis and troubleshooting window.
- **Used by chapters**: 4 (owner)

### Health check
- **Owner chapter**: `5-health-checks`
- **Canonical definition**: A **health check** is an endpoint (typically `/healthz`) that reports the operational status of an application and its critical dependencies; it returns quickly with an HTTP status code and optional JSON body indicating whether the service is healthy, degraded, or unhealthy. Container orchestrators use health checks to decide whether to keep traffic routing to a replica.
- **Used by chapters**: 5 (owner), 6

### Liveness probe
- **Owner chapter**: `5-health-checks`
- **Canonical definition**: A **liveness probe** is a lightweight health check that verifies the process is still running and responsive; it typically checks only that the application started successfully without calling external dependencies. Container orchestrators poll liveness regularly and kill and restart the container if it reports unhealthy, preventing zombie processes from accumulating traffic.
- **Used by chapters**: 5 (owner)

### Readiness probe
- **Owner chapter**: `5-health-checks`
- **Canonical definition**: A **readiness probe** is a health check that verifies the application is ready to serve traffic, often by testing whether critical external dependencies (database, cache, downstream API) are reachable. Container orchestrators gate traffic to a replica until readiness passes, preventing requests from reaching an incompletely initialized instance.
- **Used by chapters**: 5 (owner)

### Dependency probe
- **Owner chapter**: `5-health-checks`
- **Canonical definition**: A **dependency probe** (or dependency health check) is a check that verifies an external service or resource the application depends on is reachable and responding; examples include database connectivity checks and HTTP GET requests to a downstream API endpoint. Dependency probes are commonly used in readiness checks to gate traffic until external systems are available.
- **Used by chapters**: 5 (owner)

### /healthz endpoint
- **Owner chapter**: `5-health-checks`
- **Canonical definition**: The **/healthz endpoint** is a Kubernetes-convention HTTP endpoint (usually returning `200 OK` or `503 Service Unavailable`) that exposes health status of an application. It is called by orchestrators and monitoring systems to determine replica health and is often separated from the main application controllers to avoid coupling liveness/readiness logic with business logic.
- **Used by chapters**: 5 (owner)

### Alert rule
- **Owner chapter**: `6-alerts-and-slos`
- **Canonical definition**: An **alert rule** is an automated condition (e.g., "error count > 10 in the last 5 minutes") that, when met, triggers a notification to one or more action groups. Alert rules are configured in Application Insights, Log Analytics, or Azure Monitor and allow teams to be notified of problems without constantly checking dashboards.
- **Used by chapters**: 6 (owner)

### Action group
- **Owner chapter**: `6-alerts-and-slos`
- **Canonical definition**: An **action group** is a collection of notification channels (email, SMS, webhook, PagerDuty, Slack) that an alert rule sends to when triggered. A single action group can be reused across multiple alert rules, centralizing notification routing and enabling teams to manage escalation paths in one place.
- **Used by chapters**: 6 (owner)

### Threshold
- **Owner chapter**: `6-alerts-and-slos`
- **Canonical definition**: A **threshold** is the numeric boundary that, when crossed by a metric or count, triggers an alert; examples include "CPU > 80%," "response time > 500ms," or "error count >= 5." Thresholds must balance sensitivity (catching problems early) against false positives (alert fatigue); SLOs inform sensible threshold choices.
- **Used by chapters**: 6 (owner)

### SLI (Service Level Indicator)
- **Owner chapter**: `6-alerts-and-slos`
- **Canonical definition**: An **SLI** (Service Level Indicator) is a measured, quantitative metric that describes a desired aspect of service performance; examples include "percentage of successful requests," "p99 response latency," and "uptime percentage." SLIs are the foundation of SLOs and error budgets — they are what you measure.
- **Used by chapters**: 6 (owner)

### SLO (Service Level Objective)
- **Owner chapter**: `6-alerts-and-slos`
- **Canonical definition**: An **SLO** (Service Level Objective) is a target value or range for an SLI over a specified period; an example is "99.9% of requests succeed over a 30-day month." SLOs guide resource allocation, inform alerting thresholds, and establish the acceptable failure rate (error budget) for the service.
- **Used by chapters**: 6 (owner)

### Error budget
- **Owner chapter**: `6-alerts-and-slos`
- **Canonical definition**: An **error budget** is the inverse of an SLO, expressing the acceptable failure rate as a quantity; for an SLO of 99.9% availability over 30 days, the error budget is 0.1%, which amounts to roughly 43 minutes of acceptable downtime. Error budgets guide decisions about whether to ship new features or focus on reliability.
- **Used by chapters**: 6 (owner)

## Terms borrowed from earlier Parts

### HTTP / Status code
- **Defined in**: Part III — Application Development / `1-http-fundamentals`
- **Reference link**: `/course-book/3-application-development/1-http-fundamentals/`

### ASP.NET Core
- **Defined in**: Part III — Application Development / `2-the-dotnet-platform`
- **Reference link**: `/course-book/3-application-development/2-the-dotnet-platform/`

### Dependency injection
- **Defined in**: Part III — Application Development / `6-dependency-injection`
- **Reference link**: `/course-book/3-application-development/6-dependency-injection/`

### Azure Container Apps
- **Defined in**: Part VIII — DevOps and Delivery / `7-azure-container-apps`
- **Reference link**: `/course-book/8-devops-and-delivery/7-azure-container-apps/`

### Managed identity
- **Defined in**: Part V — Identity & Security / `8-managed-identities`
- **Reference link**: `/course-book/5-identity-and-security/8-managed-identities/`
