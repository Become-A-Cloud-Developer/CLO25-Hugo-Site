+++
title = "Log Analytics and KQL Basics"
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

## Log Analytics and KQL Basics
Part IX — Operations and Observability

---

## Why a query language
- App Insights dashboards answer the **common** questions
- Long-tail incidents need **arbitrary** questions about the same data
- A query language lets a one-off question become a one-line answer
- The data is already there — it just needs the right way to ask

---

## Workspace and tables
- A **Log Analytics workspace** is the shared time-series store
- Container Apps and Application Insights both write into it
- Data is partitioned into **tables**, one per data-source type
- `AppRequests`, `AppExceptions`, `AppDependencies`, `ContainerAppConsoleLogs_CL`

---

## KQL as a pipe of transforms
- **KQL** (Kusto Query Language) chains operators with the `|` symbol
- Each operator transforms the table flowing through it
- Read top to bottom — source, filter, narrow, aggregate, sort
- The pipeline is the natural unit of authoring and review

---

## The workhorse operators
- `where` filters rows — keep what matters, prune early
- `project` selects columns — narrows the row before downstream work
- `summarize` aggregates — `count()`, `avg()`, `percentile()` per group
- `extend` adds a computed column — `bin(TimeGenerated, 1m)` for charts

---

## Time range first, always
- A **time range** bounds cost, latency, and partitions scanned
- `where TimeGenerated > ago(1h)` belongs at the **front** of every query
- The portal time-picker is convenient but not portable
- Embed the filter so the query works in workbooks and alerts too

---

## Worked example: failed responses
- `AppRequests | where TimeGenerated > ago(1h)`
- `| where ResultCode startswith "5"`
- `| summarize FailureCount = count() by OperationName`
- `| order by FailureCount desc | take 10`

---

## Retention as a cost knob
- **Retention** sets how far back queries can reach — default **30 days**
- Configurable from 7 days to 2 years, per workspace or per table
- Longer retention costs more — pay per GB ingested **and** per GB retained
- Tune per table: keep `AppRequests` longer, drop console logs sooner

---

## Saving queries and workbooks
- **Saved queries** persist a one-off into the workspace or the user account
- **Workbooks** combine queries, charts, parameters into one investigation page
- A workbook with a date-range parameter feeds every query inside it
- Domain-specific workbooks become the team's investigation playbook

---

## Questions?
