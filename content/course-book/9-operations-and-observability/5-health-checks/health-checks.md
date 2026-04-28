+++
title = "Health Checks"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 50
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/9-operations-and-observability/5-health-checks.html)

[Se presentationen på svenska](/presentations/course-book/9-operations-and-observability/5-health-checks-swe.html)

---

A container orchestrator decides every few seconds whether to keep sending traffic to a running replica, restart it, or remove it from the load balancer. That decision is only as good as the signal the replica gives back. Without an explicit signal, the orchestrator falls back to "the TCP socket on port 8080 accepted my connection" — a check that passes for a process that has crashed mid-request, deadlocked its thread pool, or lost its database connection. The application stays in rotation while every request returns 500. The chapter develops the contract that closes this gap: an HTTP endpoint the application owns, designed to answer the orchestrator's question honestly, and the three flavours of that question that mean different things to the platform.

## Why a TCP probe is not enough

[Azure Container Apps](/course-book/8-devops-and-delivery/7-azure-container-apps/) — and Kubernetes underneath — schedules many replicas of an application across a cluster, then asks each one a recurring question: are you still able to do useful work? The default answer comes from a TCP-level probe. The platform opens a connection on the application port; if the connect succeeds, the replica is considered healthy. The check is cheap and requires no cooperation from the application, which is its only virtue.

Several common failure modes pass a TCP probe while the application is no longer functioning:

- The HTTP server thread is alive and accepting connections, but every request hangs because the database connection pool is exhausted.
- The application boots, binds the port, and then a background initialiser fails — a missing Key Vault secret, a Mongo cluster the network rules forbid — and the app is permanently in a half-initialised state.
- A garbage collection pause or an unhandled exception in the request pipeline leaves the process technically alive but practically unable to answer.

Each of these is a real outage from the user's point of view, and none of them are visible to a TCP probe. The remedy is for the application to expose its own opinion of its health and for the platform to ask for that opinion instead.

## What a health check is

A **health check** is an endpoint (typically `/healthz`) that reports the operational status of an application and its critical dependencies; it returns quickly with an HTTP status code and optional JSON body indicating whether the service is healthy, degraded, or unhealthy. Container orchestrators use health checks to decide whether to keep traffic routing to a replica.

The contract is small and deliberately blunt:

- A successful check returns `200 OK`. The platform reads "healthy" and keeps the replica in rotation.
- A failed check returns `503 Service Unavailable`. The platform reads "do not send me traffic right now" and acts accordingly.
- An optional JSON body lists the individual sub-checks and their results, useful for humans debugging a failure but ignored by the orchestrator's polling loop.

The endpoint's job is to be fast and cheap. A health check that takes 30 seconds to compute is itself a source of outage: the platform's probe times out, marks the replica unhealthy, and removes it from the load balancer even though it was working fine. Probes typically run every few seconds, so the budget for a single check is on the order of a few hundred milliseconds, including any dependency calls it chooses to make.

## The three canonical probe types

A single endpoint cannot answer every operational question, because the platform takes different action depending on what kind of failure it sees. Container Apps and Kubernetes both expose three distinct probe types, each tied to a specific corrective response.

### Liveness — is the process running

A **liveness probe** is a lightweight health check that verifies the process is still running and responsive; it typically checks only that the application started successfully without calling external dependencies. Container orchestrators poll liveness regularly and kill and restart the container if it reports unhealthy, preventing zombie processes from accumulating traffic.

The defining feature of a liveness probe is what it does *not* check. It must not call the database, must not reach out to Key Vault, must not depend on a downstream API. If any of those is unreachable, killing and restarting the container will not help — the new container will fail liveness for the same reason the old one did, and the platform will enter a restart loop that solves nothing and burns the replica budget. Liveness exists to recover from in-process faults: a deadlocked thread pool, a corrupted in-memory state, a managed crash that left the process up but unresponsive.

A liveness check that returns `200` from a single in-memory branch is the right design. The implementation is essentially "if my code is still executing this line, I am alive."

### Readiness — can the process accept traffic right now

A **readiness probe** is a health check that verifies the application is ready to serve traffic, often by testing whether critical external dependencies (database, cache, downstream API) are reachable. Container orchestrators gate traffic to a replica until readiness passes, preventing requests from reaching an incompletely initialised instance.

A readiness failure is not a crash. The replica is fine; it just cannot serve traffic at this moment. Maybe it is still warming its caches, maybe the database is briefly unreachable, maybe it has been told to drain. The platform's response is to remove the replica from the load balancer until readiness passes again — but it does not restart the container, because restarting would not change anything.

The split between liveness and readiness is the key concept of the chapter. The same JSON outcome ("unhealthy") leads to two completely different platform actions depending on which probe reported it:

| Probe | Failure means | Platform action |
|-------|---------------|-----------------|
| Liveness | The process is broken | Kill and restart the container |
| Readiness | The process is fine but cannot serve traffic | Remove from load balancer until readiness passes |

Confusing the two is one of the most common production incidents in containerised applications. A team writes a single `/healthz` endpoint that calls the database and registers it as both liveness and readiness. The database has a brief blip. Every replica fails liveness simultaneously. The platform restarts every replica simultaneously. The new replicas come up, find the database still unreachable, fail liveness again, and the cluster enters a cascading restart loop. The same wiring with the database check on readiness only would have removed the replicas from the load balancer for thirty seconds and put them back when the database recovered.

### Dependency probe — are downstream services reachable

A **dependency probe** (or dependency health check) is a check that verifies an external service or resource the application depends on is reachable and responding; examples include database connectivity checks and HTTP GET requests to a downstream API endpoint. Dependency probes are commonly used in readiness checks to gate traffic until external systems are available.

Dependency probes are the building blocks readiness uses. A readiness endpoint typically aggregates several dependency probes — one for the database, one for the message queue, one for the secret store — and reports the worst result. If any critical dependency is unhealthy, the application is not ready to serve traffic.

The choice of which dependencies are "critical" is a design decision. A read-only API might list its database as critical and its email provider as non-critical: it can serve reads without sending mail. A write-heavy API would invert that hierarchy. The application owns this judgement; the platform only sees the rolled-up answer.

## The /healthz endpoint convention

The **/healthz endpoint** is a Kubernetes-convention HTTP endpoint (usually returning `200 OK` or `503 Service Unavailable`) that exposes health status of an application. It is called by orchestrators and monitoring systems to determine replica health and is often separated from the main application controllers to avoid coupling liveness/readiness logic with business logic.

The path is conventional, not mandatory. The trailing `z` is a Google-internal joke that escaped into the wider Kubernetes ecosystem, originally chosen to make the path unlikely to collide with a real business URL. [ASP.NET Core](/course-book/3-application-development/2-the-dotnet-platform/), Spring Boot, Express, and most other web frameworks default to `/healthz` or accept it as the documented path; orchestrator templates assume it. Using the convention removes a configuration step.

A common pattern is to expose two separate paths: `/healthz/live` for liveness and `/healthz/ready` for readiness. Each path runs only the checks appropriate to its role. The platform configuration then points the liveness probe at `/healthz/live` and the readiness probe at `/healthz/ready`, and the two responsibilities never tangle.

## ASP.NET Core health checks

ASP.NET Core ships a first-class health checks subsystem. The pattern centres on the `IHealthCheck` interface — a single method that returns a `HealthCheckResult` of `Healthy`, `Degraded`, or `Unhealthy`. Custom checks implement this interface; framework-provided checks for common dependencies (SQL Server, Mongo, Redis, Azure Key Vault) implement it on the application's behalf and are registered through extension methods on the service collection.

### Worked example: registering a Mongo dependency probe

The setup needs three pieces: registering checks in the DI container, mapping the endpoints in the request pipeline, and tagging which checks belong to which probe.

```csharp
// Program.cs

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
    .AddMongoDb(
        sp => sp.GetRequiredService<IMongoClient>(),
        name: "mongo",
        tags: new[] { "ready" })
    .AddAzureKeyVault(
        new Uri(builder.Configuration["KeyVault:Uri"]!),
        new DefaultAzureCredential(),
        options => { },
        name: "keyvault",
        tags: new[] { "ready" });

var app = builder.Build();

app.MapHealthChecks("/healthz/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
```

Three things are happening. The `AddCheck("self", ...)` call registers an in-memory check tagged `live` — this is the liveness probe, returning healthy unconditionally because the only failure mode it cares about is "this code is no longer executing." The `AddMongoDb` and `AddAzureKeyVault` calls register dependency probes tagged `ready`; each opens a short-lived connection to its target and reports whether the round-trip succeeded. The two `MapHealthChecks` calls expose the endpoints, each one filtering by tag so that the liveness path only runs the `live`-tagged checks and the readiness path only runs the `ready`-tagged ones. A failure in Mongo will fail readiness but not liveness, and the platform will remove the replica from the load balancer without restarting it — exactly the desired behaviour.

## Health checks and the smoke test gate

The deployment pipeline already has a smoke test gate (covered in [Build, Test and Smoke Gates](/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates/)) that runs after `az containerapp update` to confirm the new revision is actually serving traffic before the pipeline reports success. The natural target for that smoke test is the `/healthz/ready` endpoint of the new revision.

The composition is clean: the readiness endpoint is the application's own answer to "am I ready to serve traffic," and the smoke test is the pipeline asking that exact question once before declaring the deployment complete. A `200` from `/healthz/ready` proves three things at once — the new image was pulled, the new container booted, and every dependency the application declared critical is reachable from the new replica's network. A failed smoke test rolls the deployment back without any user ever seeing a 503.

The same endpoint serves both the orchestrator's continuous polling and the pipeline's one-shot gate, which is the point of standardising on a single contract.

## How probes integrate with autoscaling

Container Apps and Kubernetes both factor probe results into their autoscaling decisions, which is a third reason to keep liveness and readiness distinct. A replica that fails readiness is not counted as serving capacity by the load balancer, which means an autoscaler watching "average request count per healthy replica" will see the load concentrated on the remaining replicas and may scale out. A replica that fails liveness is being restarted, which the autoscaler treats as transient — restarting replicas are not new capacity. Mixing the two leaks signal across the boundary and produces erratic scaling behaviour.

## Cross-link to the exercise

The companion exercise chapter, [Logging and Monitoring](/exercises/3-deployment/10-logging-and-monitoring/), runs a Container Apps deployment end-to-end with structured logging, Log Analytics, and Application Insights. Health checks slot into that same deployment as the next observability layer: the application already publishes structured logs and telemetry; adding a `/healthz` endpoint gives the platform a way to act on the application's own self-assessment, not just consume its logs after the fact.

## Summary

A health check is an HTTP endpoint the application owns, used by container orchestrators to decide whether a replica should keep receiving traffic, be restarted, or be removed from the load balancer. The default TCP-connect probe misses every failure mode in which the process is alive but unable to do useful work, which is why an application-owned check is the standard contract in Container Apps and Kubernetes. The three canonical probes are liveness (is the process running, used for restart decisions), readiness (can the process accept traffic right now, used to gate the load balancer), and dependency probes (are downstream services reachable, used as the building blocks of readiness). ASP.NET Core implements this contract through the `IHealthCheck` interface and the `AddHealthChecks()` registration pattern, with the `/healthz` endpoint convention exposing the result. The same endpoint serves the orchestrator's continuous polling and the pipeline's smoke test gate after deployment, which is the integration point with the wider DevOps story.
