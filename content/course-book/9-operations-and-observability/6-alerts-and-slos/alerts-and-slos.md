+++
title = "Alerts and SLOs"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 60
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/9-operations-and-observability/6-alerts-and-slos.html)

[Se presentationen på svenska](/presentations/course-book/9-operations-and-observability/6-alerts-and-slos-swe.html)

---

A dashboard that nobody watches is a dashboard that fails silently. Telemetry pipelines, structured logs, and Application Insights workbooks generate enormous quantities of evidence about a running system, but evidence sitting in a queryable store is not the same as a person being told that something is wrong. Alerts close that gap by turning a queryable signal into a phone vibrating at three in the morning. The harder problem — and the one this chapter develops — is deciding what is worth waking someone up for. That is what Service Level Objectives address.

## What an alert rule is

An **alert rule** is an automated condition (e.g., "error count > 10 in the last 5 minutes") that, when met, triggers a notification to one or more action groups. Alert rules are configured in Application Insights, Log Analytics, or Azure Monitor and allow teams to be notified of problems without constantly checking dashboards. The rule has three moving parts: an evaluation source (a metric stream or a [KQL](/course-book/9-operations-and-observability/4-log-analytics-and-kql/) query), a condition expressed against that source, and a destination — the action group — that receives the notification.

Two evaluation sources dominate. Metric-based alerts read from the pre-aggregated [metrics](/course-book/9-operations-and-observability/1-the-three-pillars/) pipeline that Azure Monitor maintains for every supported resource type. They evaluate cheaply because the metrics are already aggregated at one-minute or five-minute resolution and indexed for fast comparison; the cost is that only the metrics the platform exposes are available — for a [Container App](/course-book/8-devops-and-delivery/7-azure-container-apps/), that means CPU, memory, replica count, request count per revision, and a handful of others. Log-based alerts run a KQL query on a [Log Analytics workspace](/course-book/9-operations-and-observability/4-log-analytics-and-kql/) on a schedule (typically every five minutes) and trigger when the result set crosses a threshold. The flexibility is total — anything KQL can express becomes an alertable condition — but each evaluation costs a query, and the ingestion delay of one to three minutes adds latency between event and notification.

### Threshold-based vs dynamic baseline alerts

The simpler form of alert rule uses a static threshold. A **threshold** is the numeric boundary that, when crossed by a metric or count, triggers an alert; examples include "CPU > 80%," "response time > 500ms," or "error count >= 5." Thresholds must balance sensitivity (catching problems early) against false positives (alert fatigue); SLOs inform sensible threshold choices. Static thresholds work well when the operator knows the system's normal range — a service that has consistently served below 200ms response times for six months can have a threshold set at 500ms with confidence that crossings represent real degradation.

Static thresholds fail when normal behaviour is itself variable. A retail service with five times the traffic on Friday evenings will trip a "request rate > X" alert every Friday under any threshold sensitive enough to catch a real anomaly on a Tuesday morning. Dynamic baseline alerts address this by replacing the fixed number with a model of historical behaviour. Azure Monitor learns the metric's typical pattern over the past ten days and fires only when the current value falls outside the expected band, accounting for time-of-day and day-of-week seasonality. The trade-off is that dynamic baselines need data to learn from — they perform poorly on services with under two weeks of history and on metrics that change distribution after a deployment.

## Action groups as the contact-list abstraction

When the rule fires, something has to happen. An **action group** is a collection of notification channels (email, SMS, webhook, PagerDuty, Slack) that an alert rule sends to when triggered. A single action group can be reused across multiple alert rules, centralizing notification routing and enabling teams to manage escalation paths in one place.

The separation matters operationally. A team that hand-codes a destination email into every alert rule has to update fifty rules when the on-call rotation changes hands. A team that points fifty rules at the action group `oncall-payments` updates one resource and the routing change propagates everywhere. Action groups also encapsulate escalation: the first notification can fire to a Teams channel, a follow-up sixty seconds later can SMS the on-call engineer, and a third can page a manager if the alert is still active after fifteen minutes.

Channel choice is not cosmetic. An email is appropriate for a daily-digest alert about disk usage trending upwards; an SMS is appropriate for an alert that needs human action within the hour; a webhook into PagerDuty is appropriate for a system that already has an incident-management workflow defined. Routing every alert to email guarantees that the urgent ones get lost in the noise of the routine ones — the channel itself is part of the signal.

## The alert fatigue problem

A team that adds an alert rule for every metric eventually hits alert fatigue: the on-call engineer starts ignoring notifications because most of them turn out to be nothing. Once that habit forms, the genuine incidents get ignored too, and the alerting system becomes worse than no alerting at all — it generates the false confidence that someone is watching.

Alert fatigue has two causes. The first is too many rules: every metric gets a threshold, every threshold gets a notification, and the notification volume swamps the operator's attention. The second is rules that fire on transient conditions that resolve themselves before anyone can act — a thirty-second CPU spike during a deployment, a single failed health probe during a node replacement, a brief blip in error rate during a transient dependency failure. Both causes have the same fix at the rule level (require the condition to hold for a duration before firing) but the deeper question is which conditions are worth firing on at all.

That is where Service Level Objectives come in. SLOs replace the question "what can we measure" with "what does our user actually care about," and the discipline of choosing one shapes the alerting strategy that follows.

## SLI, SLO, and error budget

Three terms travel together. The **SLI** (Service Level Indicator) is a measured, quantitative metric that describes a desired aspect of service performance; examples include "percentage of successful requests," "p99 response latency," and "uptime percentage." SLIs are the foundation of SLOs and error budgets — they are what you measure. The SLI is purely descriptive; it does not say what is good, only what was observed.

The **SLO** (Service Level Objective) is a target value or range for an SLI over a specified period; an example is "99.9% of requests succeed over a 30-day month." SLOs guide resource allocation, inform alerting thresholds, and establish the acceptable failure rate (error budget) for the service. The SLO is the contract — the number the team commits to deliver against.

The **error budget** is the inverse of an SLO, expressing the acceptable failure rate as a quantity; for an SLO of 99.9% availability over 30 days, the error budget is 0.1%, which amounts to roughly 43 minutes of acceptable downtime. Error budgets guide decisions about whether to ship new features or focus on reliability. The error budget reframes reliability as a finite resource that gets consumed rather than as a binary "is the system up." Over a thirty-day window, the team can spend its budget on a controlled-rollout incident, on a planned maintenance window, or on the slow drip of intermittent failures — the question is whether any spending pattern will exhaust the budget before the window resets.

| Concept | What it is | Example |
|---------|------------|---------|
| SLI | The measurement | 99.92% of HTTP requests returned 2xx or 3xx in the last 30 days |
| SLO | The target | 99.9% of HTTP requests must return 2xx or 3xx, measured over 30 days |
| Error budget | The remaining failure allowance | 0.08% (out of 0.1% budget); ~35 minutes of further downtime tolerable this window |

### Alerting on the error budget

The leverage of this vocabulary is that alerts can fire on the budget rather than on raw failure events. A naive rule says "alert when the error rate exceeds 1% over five minutes." A budget-aware rule says "alert when the current burn rate would exhaust the thirty-day error budget in less than two days." The second formulation tolerates short, isolated spikes that are within the budget while still catching sustained degradation that threatens the SLO. The result is fewer alerts, each carrying more meaning.

This is also why setting realistic SLOs matters. An SLO chosen at 99.99% (52 minutes of downtime per year) on a service that has historically delivered 99.5% will trigger constantly, because every normal week consumes the entire annual budget. Start by measuring the SLI for several weeks before promising any number — the baseline measurement is the prerequisite for choosing a target that is both meaningful to users and achievable by the team. An SLO that the system cannot meet under normal conditions is worse than no SLO at all; it teaches everyone that the number is a fiction.

## A worked example: alerting on 5xx error rate

Consider a Container App serving HTTP traffic, instrumented with [Application Insights](/course-book/9-operations-and-observability/3-application-insights/) so request telemetry flows into the workspace. The team has measured the SLI for two weeks and observed a baseline 5xx rate of around 0.05%, with brief spikes during deployments. They commit to an SLO of 99.5% successful responses over a thirty-day window — an error budget of 0.5%, or roughly three and a half hours of tolerable failures per month.

The alerting strategy has two layers. A fast burn-rate alert catches catastrophic failures within minutes; a slow burn-rate alert catches sustained degradation that would silently exhaust the budget over hours. The fast alert is the one wired in this exercise.

```bash
az monitor metrics alert create \
  --name "high-5xx-rate" \
  --resource-group rg-clo25-obs \
  --scopes /subscriptions/.../containerApps/web-app \
  --condition "avg requests/failed > 5 where ResultCode startswith '5'" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --severity 2 \
  --action ag-oncall-payments
```

The rule evaluates every minute over a five-minute sliding window. The condition is "more than five percent of requests in the window returned a 5xx status code" — a single isolated 5xx will not trip it, nor will a thirty-second blip during a deployment, but a sustained failure across multiple replicas for several minutes will. The action group `ag-oncall-payments` defines what happens next: a Teams notification to the engineering channel, an SMS to the primary on-call, and a fallback email to the team distribution list after fifteen minutes if the alert is still active.

The companion exercise [Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/) provides the deployed Container App, the Log Analytics workspace, and the Application Insights resource that make this rule possible. The exercise stops short of wiring the alert; the chapter establishes why the wiring matters before the lab.

## How SLOs counter alert fatigue in practice

Returning to the fatigue problem with the SLO vocabulary in hand: a team that has agreed to a 99.5% SLO has, by definition, accepted that 0.5% of requests can fail. Any alert rule that fires before the budget is meaningfully threatened is by definition a false positive — the system is operating within agreed bounds. The SLO converts the question "is something wrong" into the more answerable "is the budget at risk," and the latter has a numeric answer.

This shifts the team's relationship to its own reliability. When the budget is healthy, the team can ship features aggressively, accept some risk, and run experiments in production. When the budget is nearly exhausted, the team focuses on reliability work and freezes risky changes until the window resets. The error budget becomes a shared instrument that aligns engineering and product priorities around an actual measurement, rather than a debate about whether the latest incident "felt bad enough" to slow down. Alerts in this regime are rare but meaningful, and on-call engineers learn to trust them.

## Summary

An alert rule pairs a condition (a query or metric threshold) with an action group that delivers a notification when the condition is met. Action groups centralize the routing of those notifications across email, SMS, webhook, and incident-management integrations, decoupling rule logic from contact lists. Static thresholds suit metrics with stable baselines; dynamic baselines handle seasonal or trending behaviour at the cost of needing historical data. The SLI/SLO/error-budget triad — measurement, target, and allowance — turns reliability from a binary state into a finite resource the team can spend, and lets alert rules fire on budget burn rather than on raw failures. Setting a realistic SLO requires measuring the SLI first; an unachievable target produces constant alerts and trains the team to ignore them. The combination — meaningful SLOs, well-routed action groups, and rules that fire on sustained budget threats — produces an alerting system the team will actually trust.
