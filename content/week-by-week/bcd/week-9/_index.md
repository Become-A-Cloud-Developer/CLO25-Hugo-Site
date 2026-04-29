+++
title = "Week 9 (v.13)"
program = "CLO"
cohort = "25"
courses = ["BCD"]
description = "Monitoring and observability: Azure Monitor, structured logging, Application Insights"
weight = 9
+++

# Week 9 (v.13) — Monitoring

Make your application observable. Emit structured logs, ship them to Log Analytics, and use Application Insights and Azure Monitor to find and diagnose problems in production.

## Theory

- [Part IX — Operations and Observability](/course-book/9-operations-and-observability/)
  - [The Three Pillars](/course-book/9-operations-and-observability/1-the-three-pillars/the-three-pillars/) — logs, metrics, and traces
  - [Structured Logging](/course-book/9-operations-and-observability/2-structured-logging/structured-logging/)
  - [Application Insights](/course-book/9-operations-and-observability/3-application-insights/application-insights/)
  - [Log Analytics and KQL](/course-book/9-operations-and-observability/4-log-analytics-and-kql/log-analytics-and-kql/)
  - [Health Checks](/course-book/9-operations-and-observability/5-health-checks/health-checks/)
  - [Alerts and SLOs](/course-book/9-operations-and-observability/6-alerts-and-slos/alerts-and-slos/)

## Practice

- [Deployment — Monitoring VMs with Azure Monitor](/exercises/3-deployment/5-monitoring-vms-with-azure-monitor/) — VM-level metrics, alerts, dashboards
- [Deployment — Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/) — structured logging with `ILogger<T>`, container logs to Log Analytics, Application Insights

## Preparation

- Read up on Azure Monitor

## Reflection Questions

- What is monitoring and why is it important?
- How does Azure Monitor work?
- What is the difference between logs and metrics?
- Why is structured logging better than free-form text?

## Links

- [Azure Monitor Documentation](https://learn.microsoft.com/azure/azure-monitor/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
