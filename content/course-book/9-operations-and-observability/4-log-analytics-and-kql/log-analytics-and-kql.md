+++
title = "Log Analytics and KQL Basics"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 40
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/9-operations-and-observability/4-log-analytics-and-kql.html)

[Se presentationen på svenska](/presentations/course-book/9-operations-and-observability/4-log-analytics-and-kql-swe.html)

---

The dashboards in [Application Insights](/course-book/9-operations-and-observability/3-application-insights/) — Live Metrics, Application Map, Failures, the Performance blade — answer the questions that come up most often in incident response. They show request rate, the slowest dependencies, the most common exception types, and the topology of services that talk to each other. What they cannot answer is the long tail of questions that occur exactly once: "show every request to `/Home/Boom` from the last forty minutes whose response took longer than two seconds, grouped by the replica that served it." For that class of question, the dashboards are the wrong shape; they were designed by someone who could not anticipate the specific incident at hand. The right shape is a query language that runs against the same data the dashboards render, and that is what this chapter develops.

## Where the data lives

Application Insights and [Container Apps](/course-book/8-devops-and-delivery/7-azure-container-apps/) look like separate products in the Azure portal, but they write to the same physical store. A **Log Analytics workspace** is a multi-tenant storage container in Azure where multiple services (Container Apps, Application Insights, VMs, and others) send their telemetry; data is organized into tables, each scoped to a service type. The workspace provides a single point for retention policy, role-based access control, and KQL-based querying across all ingested data.

A workspace is provisioned as a regional Azure resource and given a name; from that point on, every service that writes telemetry into it does so by referencing the workspace ID. Container Apps environments create one automatically when the environment is provisioned, and stream container stdout and stderr into it. Application Insights components in workspace-based mode (the default since 2021) persist their request, dependency, exception, and trace telemetry into the same workspace. The result is a single store where a single query can correlate `ILogger` lines from a controller with the request span the SDK auto-instrumented for it.

The "workspace" is not a physical machine; it is a logical scope over a managed time-series database (the same engine that backs Azure Data Explorer). Data is indexed on ingestion, retained for a configurable window, and queried through one query interface regardless of which service emitted it.

## Tables as the unit of organization

Inside a workspace, data is partitioned into tables. A **table** (in Log Analytics) is a named collection of rows, each with the same schema, representing a single data source type (e.g., `ContainerAppConsoleLogs` for container stdout, `InsightsMetrics` for Application Insights metrics). Queries select one table as a source and apply operators to filter, project, and aggregate its rows.

A handful of tables carry most of the traffic for a containerized .NET application:

- `AppRequests` — every HTTP request the application served, with `Url`, `ResultCode`, `DurationMs`, `OperationName`, and the operation/correlation IDs that tie a request to its descendants.
- `AppExceptions` — every exception captured by the Application Insights SDK, with `Type`, `Message`, the outer stack frame, and the operation ID of the request that produced it.
- `AppDependencies` — outbound calls the application made (SQL queries, HTTP requests to other services, Azure SDK calls), each with a duration and a success flag.
- `AppTraces` — the `ILogger` lines the SDK forwarded into App Insights, with the original message template, the rendered message, the severity, and the structured fields as a JSON blob.
- `ContainerAppConsoleLogs_CL` — the raw stdout and stderr the platform captured from each replica, with `ContainerName_s`, `ContainerImage_s`, `RevisionName_s`, and the original log line in `Log_s`.

The `_CL` suffix denotes a custom log table (the legacy Container Apps schema), and the `_s` suffix on individual columns denotes a string-typed extracted field. Newer Container Apps environments expose a managed equivalent without the suffixes (`ContainerAppConsoleLogs`); both shapes exist in real workspaces, and a query written for one will silently return zero rows against the other. Confirming the table schema before writing the first query is part of the workflow.

## KQL as a pipe of transforms

The query language for these tables is **KQL** (Kusto Query Language) — a functional, case-insensitive query language used in Azure Data Explorer and Log Analytics; it uses a pipe (`|`) composition model where each operator transforms the table flowing through it, enabling users to filter, aggregate, and visualize telemetry data through expressive, readable queries that read like English.

The mental model is the Unix shell pipeline rather than SQL. A query starts with the name of a table — `AppRequests`, on its own, is already a valid query that returns every row in the table — and successive operators are chained with the pipe character, each consuming the table that flows into it and producing a new table for the next operator to consume. Reading top to bottom describes the data's journey: source, filter, narrow, aggregate, sort, limit. The engine plans the whole pipeline at once, but the pipeline is the natural unit of authoring and review.

```text
AppRequests
| where TimeGenerated > ago(1h)
| where ResultCode startswith "5"
| project TimeGenerated, OperationName, ResultCode, DurationMs
| order by TimeGenerated desc
| take 50
```

Five operators, each doing one thing: pick a time window, keep only server errors, narrow to the columns of interest, sort newest-first, and stop after fifty rows. The query is its own documentation.

## The workhorse operators

A small set of operators carries most queries written in practice. Investing in fluency with these pays back faster than learning the long tail.

A **query operator** (in KQL) is a function that transforms a table flowing into it and outputs a transformed table; common operators include `where` (filters rows), `project` (selects columns), `summarize` (aggregates), `order by` (sorts), `take` (limits rows), and `extend` (adds computed columns). Operators are chained via the pipe symbol to compose complex queries.

`where` filters rows by a predicate — `where ResultCode == "500"`, `where Url contains "/api/"`, `where TimeGenerated > ago(15m)`. Multiple `where` clauses are equivalent to one with `and`; placing them early in the pipeline lets the engine prune work cheaply.

`project` chooses which columns appear downstream and (optionally) renames them — `project When = TimeGenerated, OperationName, DurationMs`. Without `project`, every column the table has flows through, which is fine while exploring but expensive once a query goes into a workbook.

`summarize` is the aggregator. `summarize count() by ResultCode` collapses the table to one row per distinct `ResultCode` with a `count_` column showing how many rows had that value; `summarize avg(DurationMs), p95 = percentile(DurationMs, 95) by OperationName` produces both an average and a 95th-percentile latency per operation. `summarize` is what turns raw events into the numbers a dashboard or alert is built on.

`extend` adds a computed column without removing existing ones — `extend Bucket = bin(TimeGenerated, 1m)` rounds each row's timestamp to the nearest minute, which is the trick behind almost every time-series chart.

`count`, `top`, and `order by` are convenience operators built on the same primitives: `count` is `summarize count()`; `top 10 by DurationMs desc` is `order by DurationMs desc | take 10`. Keep them in the toolkit even when the longer form is equally valid; they read more naturally.

## Time range first, always

Every chapter on KQL eventually arrives at the same advice: filter by `TimeGenerated` before doing anything else, and keep the window as small as the question allows.

A **time range** is the window of historical data a KQL query operates on; expressed in queries as conditions like `where TimeGenerated > ago(30m)`, it controls both the scope of analysis and the query performance. Shorter time ranges are cheaper and faster; longer ranges increase cost and latency.

The mechanics matter. Log Analytics partitions table data by ingestion time, and a `where TimeGenerated > ago(1h)` clause lets the query engine read only the partitions that overlap the last hour. Without it, the engine scans the entire retention window — thirty days by default, up to two years if extended — and the query's runtime and cost scale accordingly. A query that returns in two seconds against the last hour can take minutes against a year.

The portal's time-picker control above the query editor inserts an implicit time filter at runtime, which means a query without an explicit `TimeGenerated` filter will still be scoped during interactive use. That convenience disappears the moment the query is saved into a workbook, scheduled as an alert, or run from the CLI; embedding `where TimeGenerated > ago(...)` makes the query portable.

`ago()` is the most common helper — `ago(15m)`, `ago(1h)`, `ago(7d)` — but absolute times work too: `where TimeGenerated between (datetime(2026-04-28T08:00:00Z) .. datetime(2026-04-28T09:00:00Z))` is the right form for replaying a known incident window.

## A worked example: failed responses by operation

A concrete question makes the mechanics tangible: identify which operations have been returning HTTP 5xx in the last hour, and how many failures each has produced.

```text
AppRequests
| where TimeGenerated > ago(1h)
| where ResultCode startswith "5"
| summarize FailureCount = count() by OperationName
| order by FailureCount desc
| take 10
```

The pipeline is short on purpose. `AppRequests` is the source. `where TimeGenerated > ago(1h)` shrinks the scan to the last hour. `where ResultCode startswith "5"` keeps server-side failures (`500`, `502`, `503`, `504`) and discards client errors and successes — the prefix match avoids the brittleness of enumerating each code. `summarize count() by OperationName` collapses the rows into one per distinct operation, with the count of failures alongside; the `FailureCount =` rename makes the resulting column self-describing. `order by FailureCount desc | take 10` produces the top-ten table.

The output is a small table with two columns and at most ten rows. From there the next move is usually to pick the worst operation and drill into it — `where OperationName == "POST Home/Boom"` followed by a `join` to `AppExceptions` on `OperationId` — to see the actual stack traces that produced the failures. The point is that each step of the investigation is a small additional pipe, not a new query written from scratch. The companion exercise [Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/) walks through this exact workflow against a Container App generating real traffic.

## Retention as a cost knob

Workspace storage has a price, and the price is paid twice: once on ingestion and once on retention. A workspace charges per gigabyte ingested (the data flowing in) and per gigabyte retained beyond the included retention window.

**Retention** is the length of time data is kept in a Log Analytics workspace before automatic deletion; the default is 30 days, but can be configured from 7 days to 2 years. Longer retention increases storage cost and query latency; shorter retention reduces cost but limits historical analysis and troubleshooting window.

Retention is set at the workspace level (a default for all tables) and can be overridden per table. A common pattern is to keep `AppRequests` and `AppExceptions` for ninety days because incident post-mortems often need recent history, while leaving high-volume `ContainerAppConsoleLogs_CL` at the default thirty days because the same information is also captured (in structured form) in `AppTraces`. The decision is a trade-off between the cost of storage and the cost of not being able to answer a question that arrives a month after the data was discarded.

A second knob, archive tiers, moves data older than a threshold to cheaper storage that is queryable only through a slower path; this is the right shape for compliance-driven retention where the data must exist but is rarely read. For everyday observability, sticking to the analytics tier and tuning retention per table is the simpler model.

## Saving useful queries as workbooks

A query written once during an incident is a query that has to be rewritten the next time. Log Analytics offers two persistence layers for queries that are worth keeping.

The first is a saved query — a named query attached to the user's account or to the workspace itself. A workspace-saved query is RBAC-gated (writing one requires Contributor on the workspace) and visible to anyone with read access, which makes it the right home for the queries the team agrees are canonical. A user-saved query lives in the user's account and never appears for others.

The second, and more capable, is a workbook. A workbook is a parameterized document that combines KQL queries, charts, tables, and free-form text into a single page; opening a workbook runs its embedded queries and renders the results. The "Failures Investigation" workbook that ships with Application Insights is a worked example: a date-range parameter at the top feeds the time filter for every query in the document, and the page below shows a failure-rate timechart, a top-failing-operations table, an exception-types breakdown, and a sample exceptions list, each driven by its own KQL query. A team can build domain-specific workbooks the same way — one workbook per service, or one workbook per type of incident — and reach for the right one when the situation matches.

Workbooks compose naturally with alert rules and scheduled queries, but those are the topic of the [alerts and SLOs chapter](/course-book/9-operations-and-observability/6-alerts-and-slos/) rather than this one.

## Summary

A Log Analytics workspace is the underlying time-series store that Container Apps and Application Insights both write into, organized into tables — `AppRequests`, `AppExceptions`, `AppDependencies`, `AppTraces`, and `ContainerAppConsoleLogs_CL` among them — each carrying a single data-source type. KQL queries those tables through a pipe-composition model in which a small set of operators (`where`, `project`, `summarize`, `extend`, `count`, `top`, `order by`, `take`) chains transforms one after another, top to bottom, each step narrowing or reshaping the table. Filtering by `TimeGenerated` belongs at the front of every query: it bounds cost, latency, and the partitions the engine has to read. Retention controls how far back queries can reach and is the main cost knob worth tuning per table. Useful queries graduate into saved queries or, better, into workbooks that combine queries, charts, and parameters into a reusable investigation surface.
