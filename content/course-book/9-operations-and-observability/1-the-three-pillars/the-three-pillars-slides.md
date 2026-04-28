+++
title = "The Three Pillars: Logs, Metrics, Traces"
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

## The Three Pillars: Logs, Metrics, Traces
Part IX — Operations and Observability

---

## A deployed app is a black box
- Without **telemetry**, the operator sees only "process is alive"
- A failed request is indistinguishable from a successful one
- No debugger, no breakpoint, no second chance to reproduce
- Visibility is whatever the application chooses to emit

---

## Monitoring vs. observability
- **Monitoring** answers a fixed list of questions ("is CPU > 80%?")
- **Observability** lets you ask new questions of existing signals
- Monitoring catches failures you predicted
- Observability investigates the ones you did not

---

## Logs — the narrative
- Timestamped, **discrete events** with full context
- One log line = one event for one user, with stack trace and IDs
- Best at: per-request detail, exception analysis
- Falls short: aggregate questions over high volume are expensive

---

## Metrics — numbers over time
- **Numeric samples** aggregated into time buckets
- Counters, gauges, histograms — pre-aggregated by construction
- Best at: dashboards, alerts, SLOs, long-term trends
- Falls short: cannot describe individual requests

---

## Traces — flow across services
- Causal chains tied by a shared **trace ID** (operation ID)
- A trace is a tree of **spans**: root span + child spans per call
- W3C Trace Context propagates the ID across HTTP hops
- Best at: latency attribution, cross-service investigation

---

## Cost trade-offs
- **Logs** — expensive at volume, indispensable for detail
- **Metrics** — cheapest at scale; cost grows with cardinality, not events
- **Traces** — medium cost; sampling preserves diagnostic value
- A working stack mixes all three at different sample rates

---

## Worked example: a failing checkout
- **Metric** — `http_requests_failed{route="/checkout"}` spikes at 14:03
- **Trace** — root span 4.8s; child span timed out calling payment provider
- **Log** — operation ID pulls the exception, stack trace, user ID
- One incident, three views — none sufficient alone

---

## How the three combine
- **Metrics** notice the incident and bound its scope
- **Traces** locate the failing dependency
- **Logs** explain the exact failure for the affected user
- Shared correlation IDs let the investigator pivot between signals

---

## What Part IX covers
- `ILogger<T>` and structured logging in ASP.NET Core
- Application Insights as the .NET telemetry sink
- Log Analytics and KQL for queryable centralised logs
- Health checks, alerts, and service-level objectives

---

## Questions?
