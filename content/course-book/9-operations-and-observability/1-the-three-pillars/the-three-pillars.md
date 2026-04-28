+++
title = "The Three Pillars: Logs, Metrics, Traces"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 10
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/9-operations-and-observability/1-the-three-pillars.html)

[Se presentationen på svenska](/presentations/course-book/9-operations-and-observability/1-the-three-pillars-swe.html)

---

A deployed application is a black box. Once the binary leaves a developer's laptop and starts serving users in the cloud, the only visibility into its behaviour is whatever the application chooses to emit. If it emits nothing, the operator can confirm the process is alive — and almost nothing else. The request that returned a 500 error to a user this morning is, from the operator's seat, indistinguishable from a request that succeeded. The instinct to attach a debugger does not survive the transition to production: there is no debugger to attach, no breakpoints to set, and no second chance to reproduce a fault that already happened.

This chapter develops the three signal types that close that gap — logs, metrics, and traces — and shows how each one answers a different question. The companion exercise [Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/) is where these pillars become concrete on a running .NET application; the prose here establishes the vocabulary and the trade-offs that the exercises assume.

## From monitoring to observability

The older discipline of monitoring asks a fixed set of questions of a system: is the CPU above 80%? Is the disk full? Has the website returned 5xx errors more than three times in the last minute? Each question is encoded as a check, each check has a threshold, and the dashboard lights up when a threshold is crossed. Monitoring is excellent at telling you that something broke. It is poor at telling you why.

**Observability** is the ability to infer the internal state of a system from the outputs it produces — logs, metrics, and traces. A system is observable when the questions asked of it can be answered without adding new instrumentation, relying instead on the signals the system already emits. The shift from monitoring to observability is a shift from a fixed list of dashboards to an open-ended capacity to ask new questions about behaviour the developers did not anticipate. Monitoring catches the failure modes you predicted; observability lets you investigate the ones you did not.

The collective term for the data that makes observability possible is **telemetry** — the structured logs, metrics, traces, and events that a system emits for external collection and analysis. Telemetry enables visibility into running applications without changing the code to add logging statements or metrics collection. The application emits a stream; a backend collects, indexes, and queries that stream. The investigator's tools sit on the backend, not on the running process.

## Logs: the narrative of what happened

**Logs** are timestamped, structured records of discrete events that occurred in the system; they form the narrative of what happened, in what order, with what context attached. Unlike metrics, logs capture the full detail of a single event (the error message, the exception stack, the request ID); unlike traces, logs exist within a single process and do not automatically cross service boundaries.

A well-written log line for a checkout request might record the user ID, the cart total, the payment provider, the response code, and a correlation ID that ties all log lines from the same request together. Replaying that line three months later, the investigator can reconstruct what the application was doing at that exact moment for that exact user. No other signal type carries this level of per-event detail.

Logs excel at narrative reconstruction — "what exactly happened to user 42 at 14:03 yesterday?" — and at exception analysis, where the stack trace is the load-bearing payload. They are also the signal that survives the developer's intuition of what to instrument: a `Warning` log line written eighteen months ago for a once-in-a-blue-moon edge case is still there when the edge case finally fires.

The cost of this richness is volume. Every event becomes a row in a log store. A modest web application emits tens of thousands of log lines per minute; a busy one emits millions. Log Analytics, Datadog, Splunk, and other backends charge per gigabyte ingested and per gigabyte retained. Aggregating logs to answer "what was the average response time over the last hour?" forces the query engine to scan and group every relevant row — expensive, and slower than asking the same question of a metric.

## Metrics: numbers over time

**Metrics** are numeric measurements sampled at regular intervals (e.g., every second or minute) that describe the state or behaviour of a system; examples include CPU usage, request count per minute, and response time percentiles. Metrics are cheap to aggregate and store, making them ideal for answering "how is the system performing in aggregate" but poor at answering "what was the exact sequence of events."

A metric is an aggregate by construction. The series `http_requests_total` does not record individual requests; it records the count of requests in each one-minute bucket, possibly split by status code or route. The series `http_request_duration_seconds_p95` does not record any individual request's latency; it records the 95th percentile of all request durations in each bucket. The aggregation is the data — there is no underlying detail to drill into.

This shape makes metrics dramatically cheaper to store and query than logs. A counter that increments a billion times per day still produces only a few thousand data points (one per minute, or one per ten seconds for higher-resolution series). A dashboard rendering a week of CPU usage scans a few thousand rows; the equivalent in logs would scan billions.

Metrics are the signal of choice for dashboards, alerts, and capacity planning. A graph of error rate over the last hour, a threshold that fires when latency exceeds 500 ms, a long-term trend that shows traffic doubling year-over-year — these are metric problems. Metrics are also the foundation of service-level objectives (SLOs): the SLO "99.9% of requests succeed over a 30-day window" is computed from a request-success counter, not from a search of the log store.

The trade-off cuts the other way for diagnosis. A metric tells you the error rate spiked at 14:03; it cannot tell you which user was affected, what they were trying to do, or what the stack trace looked like. For that, the investigator needs a different signal.

## Traces: end-to-end flow across services

**Traces** (in the distributed tracing sense) are causal chains of operations that flow through multiple services, tied together by a shared correlation ID (operation ID); they answer "how did a single user request flow through the system and where did time get spent." A trace includes one or more spans, each representing a unit of work (an HTTP request, a database query, a message consumption).

A **span** is a named unit of work within a distributed trace, representing a single operation; it carries a start time, end time (duration), a status (success or error), and any events or attributes that occurred during its execution. Multiple spans from different services are linked by a shared trace ID and parent-span ID to form a complete trace. The result is a tree: a root span for the inbound HTTP request, child spans for each downstream call (the SQL query, the call to a payment provider, the message published to a queue), and grandchild spans for whatever those services in turn called.

Traces close the gap that logs and metrics leave open: they make a request's journey across service boundaries visible. In a system of three services where a checkout call traverses an order service, a payment service, and a shipping service, a log line in the shipping service does not automatically know that it belongs to the same user request as a log line in the order service. A trace ties them together. The W3C Trace Context standard defines the header (`traceparent`) that propagates the trace ID across HTTP boundaries; instrumented frameworks read and write it automatically, so the correlation survives every hop.

The cost of traces is instrumentation. Every service must emit spans, and every cross-service call must propagate the trace context header. Auto-instrumentation libraries — for [ASP.NET Core](/course-book/3-application-development/2-the-dotnet-platform/), for HttpClient, for Entity Framework, for Azure SDK — cover most cases without code changes; custom logic still needs explicit spans for the work that matters. At high volume, traces are usually sampled (1% of requests, or 100% of failed requests) to keep ingestion costs bounded, with statistical reweighting to preserve aggregate accuracy.

## How the three pillars combine

No single pillar is sufficient. Logs without metrics turn every dashboard question into an expensive query. Metrics without logs reduce every incident to a graph that confirms the failure but explains nothing. Traces without either narrate the flow of one request but cannot answer "is this request typical?"

| Pillar | Best at | Falls short when |
|--------|---------|------------------|
| Logs | Narrative reconstruction; exception detail | Aggregate questions over high volume |
| Metrics | Dashboards, alerts, SLOs, trends | Per-request detail; root-cause investigation |
| Traces | Cross-service request flow; latency attribution | Storage cost at full sampling; frameworks must be instrumented |

A mature observability stack uses each for what it does best. Metrics drive the dashboards on the wall and the alerts on the pager. Traces drive the latency investigation when the dashboards report a slow endpoint. Logs drive the root-cause analysis when the trace identifies the failing span. The three pillars share a correlation key (operation ID, request ID) so that an investigator who starts in one can pivot to the others without losing the thread.

### Worked example: a single failing request

A user reports that submitting a cart fails intermittently around 14:00. The on-call engineer opens the observability stack and walks through the three pillars.

**Metric — the aggregate signal.** The dashboard shows the metric `http_requests_failed_total{route="/checkout"}` spiking from a baseline of 0–1 per minute to 47 per minute around 14:03, then settling back to baseline ten minutes later. The metric establishes the shape of the incident: a brief failure window, several dozen affected requests, route-specific. It does not say which users, which requests, or which downstream call.

```text
http_requests_failed_total{route="/checkout"}
  14:01  1
  14:02  0
  14:03 47   <-- spike
  14:04 38
  14:05 12
  14:06  0
```

The metric is the right tool for noticing the incident and characterising its scope, but the investigator now needs detail.

**Trace — the request-flow signal.** The engineer pivots to the trace store, filters to traces with status 500 on the `/checkout` route in the spike window, and opens one. The trace shows a 4.8-second root span for the inbound POST, with most of the time spent in a single child span: a call to the payment provider's `/charge` endpoint that timed out at 5 seconds.

```text
POST /checkout                   4823ms  ERROR
├── SQL SELECT cart_items          12ms  OK
├── HTTP POST payment.example      5000ms  ERROR (timeout)
└── (no further spans — request aborted)
```

The trace identifies the failing dependency: the payment provider, not the database, not the application code. It also identifies the symptom: timeouts, not 4xx responses, suggesting the provider was unresponsive rather than rejecting requests.

**Log — the per-event detail.** The engineer copies the operation ID from the trace and pivots to the log store, querying for that ID. Three log lines appear: an `Information` line at the start of the request, a `Warning` line when the HttpClient cancellation token fired, and an `Error` line with the full exception stack trace from the `TaskCanceledException`. The error log line includes the user ID, the cart contents, the request body, and the correlation ID — enough context to call the user and confirm the cart was not double-charged.

The same incident, seen through three signals, yields three different parts of the answer. Metrics said *something is wrong*. Traces said *the payment provider is the bottleneck*. Logs said *here is the exact failure for this exact user*. None of the three would have produced this complete picture alone.

## Cost trade-offs in practice

Each pillar carries a different cost shape, and a working observability budget allocates spend accordingly.

Logs are the most expensive per question answered. Storage scales with event volume, and queries scale with the time range scanned. Teams reduce log cost by sampling at the source (drop debug logs in production), retaining recent data hot and older data cold, and pushing aggregate questions to metrics instead.

Metrics are the cheapest at scale. A pre-aggregated counter costs almost nothing to store and almost nothing to query, regardless of underlying request volume. Metric cost grows with cardinality — the number of distinct label combinations — not with raw event count. A counter labelled by `status_code` is cheap; a counter labelled by `user_id` is catastrophic, because every user creates a new series.

Traces sit between the two. A fully sampled trace store has logs-like cost characteristics; a 1%-sampled store has metric-like cost with logs-like detail on the sampled subset. Adaptive sampling — keep all errors and slow requests, drop a fraction of the rest — gives most of the diagnostic value at a fraction of the price.

A reasonable starting allocation for a small ACD-scale application: metrics on every signal that drives a dashboard or alert (cheap, always on); logs at `Information` level for business events and `Warning`/`Error` for failures (medium cost); traces auto-instrumented on framework calls with adaptive sampling at higher volume (medium cost, full detail on errors).

## Summary

A deployed application emits three kinds of signal that, together, make it observable. Logs record discrete events with full per-event context, ideal for narrative reconstruction and exception detail but expensive at high volume. Metrics record numeric measurements aggregated into time buckets, cheap and ideal for dashboards, alerts, and SLOs but unable to describe individual requests. Traces record the end-to-end flow of a single request across services as a tree of spans, ideal for latency attribution and cross-service investigation. None of the three is sufficient alone: metrics notice an incident, traces locate the failing dependency, logs explain the exact failure for the affected user. The remaining chapters of this Part build on this foundation — structured logging with `ILogger<T>`, Application Insights as the .NET telemetry sink, KQL over Log Analytics, health checks, and alerts grounded in service-level objectives — each one developing a specific capability of the three-pillar model on a running .NET application.
