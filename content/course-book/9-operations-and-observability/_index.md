+++
title = "Part IX — Operations and Observability"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Operating cloud applications: the three pillars of observability, structured logging with ILogger, Application Insights, KQL, health checks, and alerts."
weight = 90
chapter = true
head = "<label>Part IX</label>"
+++

# Part IX — Operations and Observability

Once an application is deployed, it becomes a black box unless its operators have made it observable. This Part covers the signals that reveal what an application is doing in production and the tooling that turns those signals into answers: the three pillars of observability, structured logging, Application Insights, KQL queries against Log Analytics, health checks, and alerts.

The companion exercise is the [Logging and Monitoring chapter](/exercises/3-deployment/10-logging-and-monitoring/), which wires up the same `CloudCi` application progressively from container-stdout-only logging through Application Insights to KQL-based dashboards and alerts.

{{< children />}}
