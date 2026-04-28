+++
title = "Health Checks"
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

## Health Checks
Part IX — Operations and Observability

---

## Why a TCP probe is not enough
- The orchestrator decides every few seconds: keep, restart, or drain a replica
- Default probe is "did the **TCP connect** succeed" — almost always passes
- Process can be alive but **deadlocked**, **dependency-starved**, or stuck mid-init
- Application needs to publish its own opinion of its health

---

## What a health check is
- An HTTP endpoint — typically **`/healthz`** — owned by the application
- Returns **`200 OK`** when healthy, **`503 Service Unavailable`** when not
- Optional JSON body lists sub-checks for human debugging
- Must be **fast and cheap** — probe budget is a few hundred milliseconds

---

## Three canonical probe types
- **Liveness** — is the process running, used for restart decisions
- **Readiness** — can the process accept traffic right now, gates the load balancer
- **Dependency probe** — is a downstream service reachable, building block of readiness
- Each probe answers a different question and triggers a different platform action

---

## Liveness vs readiness
- **Liveness fail** → kill and restart the container
- **Readiness fail** → remove from load balancer, do not restart
- Confusing the two causes **cascading restart loops** during dependency blips
- Liveness must not call dependencies; readiness usually does

---

## The /healthz convention
- `/healthz/live` for liveness, `/healthz/ready` for readiness
- Path is conventional — Kubernetes templates and ASP.NET Core defaults assume it
- Separating paths keeps the two responsibilities from tangling
- Same contract works for Container Apps, Kubernetes, and the smoke test gate

---

## ASP.NET Core IHealthCheck
- `IHealthCheck` interface — single async method, returns Healthy / Degraded / Unhealthy
- Framework checks for **Mongo, Key Vault, SQL, Redis** ship as extension methods
- Custom checks implement the interface, get injected dependencies via DI
- **Tags** route each check to the right probe path

---

## Worked example: register Mongo and Key Vault
- `AddHealthChecks().AddCheck("self", ..., tags: ["live"])`
- `.AddMongoDb(..., tags: ["ready"])` and `.AddAzureKeyVault(..., tags: ["ready"])`
- `MapHealthChecks("/healthz/live", Predicate = c => c.Tags.Contains("live"))`
- `MapHealthChecks("/healthz/ready", ...)` filters to ready-tagged checks

---

## Health checks and the smoke test gate
- Pipeline's smoke gate already curls the new revision after `az containerapp update`
- The natural target is **`/healthz/ready`** of the new revision
- A 200 proves the image pulled, the container booted, and dependencies are reachable
- A 503 fails the gate before users see a 5xx — clean rollback

---

## Composing with the rest of Part IX
- **Logs** answer "what happened"; **health checks** drive automatic recovery
- Readiness shapes **autoscaling** — unhealthy replicas don't count as capacity
- Dependency probes surface failures that logs alone would miss
- Single endpoint serves continuous polling and one-shot deploy gates

---

## Questions?
