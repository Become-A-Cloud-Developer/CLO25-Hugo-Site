+++
title = "Week 5 (v.19)"
program = "CLO"
cohort = "25"
courses = ["ACD"]
description = "Logging and monitoring: structured logging with ILogger, Application Insights, Log Analytics, Azure Monitor"
weight = 5
+++

# Week 5 (v.19) — Logging and Monitoring

Make the deployed application observable. Emit structured logs with `ILogger<T>`, ship them to Log Analytics, and query them with KQL via Application Insights and Azure Monitor.

## Theory

- [Part IX — Operations and Observability](/course-book/9-operations-and-observability/)
  - [The Three Pillars](/course-book/9-operations-and-observability/1-the-three-pillars/the-three-pillars/) — logs, metrics, traces
  - [Structured Logging](/course-book/9-operations-and-observability/2-structured-logging/structured-logging/)
  - [Application Insights](/course-book/9-operations-and-observability/3-application-insights/application-insights/)
  - [Log Analytics and KQL](/course-book/9-operations-and-observability/4-log-analytics-and-kql/log-analytics-and-kql/)
  - [Alerts and SLOs](/course-book/9-operations-and-observability/6-alerts-and-slos/alerts-and-slos/)

## Practice

- [Deployment — Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/) — three progressive exercises
  - [Structured logging with `ILogger`](/exercises/3-deployment/10-logging-and-monitoring/1-structured-logging-ilogger/)
  - [Container logs to Log Analytics](/exercises/3-deployment/10-logging-and-monitoring/2-container-logs-to-log-analytics/)
  - [Application Insights](/exercises/3-deployment/10-logging-and-monitoring/3-application-insights/)

## Preparation

- Read up on Azure Monitor and Application Insights

## Reflection Questions

- What is structured logging and why is it better than free-form text logs?
- How do Application Insights and Log Analytics fit together?
- What is the difference between logs and metrics?

## Links

- [Azure Monitor](https://learn.microsoft.com/azure/azure-monitor)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
