+++
title = "Alerts and SLOs"
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

## Alerts and SLOs
Part IX — Operations and Observability

---

## Why alerts matter
- A dashboard that nobody watches **fails silently**
- Telemetry sitting in storage is not the same as someone being told
- Alerts turn a queryable signal into a phone vibrating
- The harder question — what is worth waking someone up for

---

## What an alert rule is
- **Alert rule** = condition + evaluation source + destination
- Evaluation source: a metric stream or a **KQL** query
- Destination: an **action group** that delivers the notification
- Configured in Azure Monitor, Application Insights, or Log Analytics

---

## Metric-based vs log-based alerts
- **Metric-based** — pre-aggregated, cheap, low latency, fixed metrics only
- **Log-based** — runs a KQL query on a schedule, total flexibility
- Log-based pays for ingestion delay (1–3 min) and per-evaluation cost
- Pick metric-based when the metric exists, log-based when it does not

---

## Static thresholds vs dynamic baselines
- A **threshold** is the numeric boundary that triggers an alert
- Static thresholds work when normal behaviour has a stable range
- Dynamic baselines learn historical patterns, handle seasonality
- Dynamic baselines need ~10 days of history to learn from

---

## Action groups
- **Action group** = a reusable collection of notification channels
- One group, many alert rules — change the rotation in one place
- Channels: email, SMS, webhook, PagerDuty, Slack, Teams
- Channel choice is part of the signal — email vs SMS vs page

---

## The alert fatigue trap
- Too many rules → on-call ignores notifications → real incidents missed
- Transient conditions (deployment spikes) trip thresholds without cause
- Adding "for N minutes" duration helps at the rule level
- The deeper fix: stop alerting on conditions users do not feel

---

## SLI, SLO, error budget
- **SLI** — the measurement (e.g. % of requests returning 2xx)
- **SLO** — the target (e.g. 99.5% over 30 days)
- **Error budget** — what is left of "allowed unavailability"
- 99.9% SLO over 30 days → ~43 min of tolerable downtime

---

## Setting a realistic SLO
- Measure the SLI for **several weeks before** promising a number
- An unachievable SLO trains the team that the number is a fiction
- The SLO must be both meaningful to users and reachable by the team
- 99.99% on a service that delivers 99.5% guarantees constant alerting

---

## Worked example: 5xx burn-rate alert
- SLO: 99.5% successful responses over 30 days
- Rule: avg `requests_failed_5xx` > 5% over a 5-minute window
- Single 5xx blips do not trip; sustained failure does
- Wired to action group `ag-oncall-payments` — Teams + SMS + email fallback

---

## Why SLOs counter fatigue
- Within budget = system is operating as agreed → no alert
- Budget at risk = real threat to the contract → alert worth trusting
- Alerts become rare but meaningful — on-call learns to trust them
- The budget aligns engineering and product around one measurement

---

## Questions?
