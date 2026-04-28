+++
title = "Azure Container Apps as a Deployment Target"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 70
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/8-devops-and-delivery/7-azure-container-apps.html)

[Se presentationen på svenska](/presentations/course-book/8-devops-and-delivery/7-azure-container-apps-swe.html)

---

A pipeline that builds, tests, and pushes an image is only half of a delivery story. The other half is the platform that receives the image and turns it into a running, addressable, scalable service. Plain virtual machines can do the job, but at the cost of patching, autoscaling, load-balancing, and certificate rotation that the team must manage by hand. A full Kubernetes cluster solves those concerns but introduces its own operational tax — node pools, ingress controllers, RBAC, etcd backups — that is hard to justify for a handful of stateless web services. Azure Container Apps occupies the gap between these two extremes: it accepts an image, runs it as a managed service with autoscaling and HTTPS ingress already wired up, and exposes only the few primitives that an application team actually needs to reason about. This chapter explains those primitives and how they compose with the pipeline pieces from earlier in this Part.

## Why a managed runtime for stateless containers

Most application services in a typical web system are stateless: they hold no durable state between requests, scale horizontally by adding replicas, and recover from a crash by being restarted on a different host. Running such services on Kubernetes works, but the operator carries the entire cluster lifecycle — upgrading the control plane, patching nodes, tuning the cluster autoscaler, configuring ingress, wiring observability — for the sake of a workload that only needs "run this container, give me a URL, and add replicas when traffic rises."

**Azure Container Apps** is a managed, serverless container service that runs Docker images without requiring container orchestration expertise; it handles scaling, networking, and revision management, making it ideal for deploying containerized applications without operational burden. The platform runs on top of Kubernetes internally, but the cluster is invisible: there are no nodes to size, no kubelets to patch, no ingress controllers to configure. The team interacts with three primitives — environments, container apps, and revisions — and lets Azure handle everything below.

The trade-off is reduced control. Container Apps imposes opinions about scaling behaviour, ingress, and lifecycle that a hand-rolled Kubernetes deployment could override. For services that fit those opinions — stateless HTTP APIs, web front-ends, background workers driven by a queue — the saved operational cost is large. For services that need a custom CNI plug-in, a service mesh sidecar, or persistent volumes with strict topology requirements, AKS or another full orchestrator is still the right answer.

## The Container Apps model

The platform is built from three layered concepts: the environment, the container app, and the revision.

A **Container App environment** is the shared infrastructure and network namespace in which one or more Container Apps run; it provides a virtual network boundary, shared logging, and a common internal DNS domain for service-to-service communication. An environment is the unit that the pipeline provisions once and then ignores. Apps inside the same environment can address each other by short DNS names; logs from every app in the environment flow into a single Log Analytics workspace; the environment's virtual network controls what can reach those apps from outside.

A **container app** is the deployable unit that runs an image. The app holds the metadata that defines a service: which image to run, what environment variables to inject, what scale rules to apply, and what ingress to expose. It belongs to exactly one environment. Most teams treat one container app as one microservice — `cloudci-api`, `cloudci-worker`, `cloudci-frontend` — and the environment as the runtime that hosts all of them.

A **revision** (in Azure Container Apps) is an immutable version of a Container App; when you deploy a new image or change configuration, a new revision is created automatically, and traffic can be split between revisions for canary or blue-green deployments. The revision is the layer where deployments actually happen. The container app is conceptually the long-lived service `cloudci`; the revision `cloudci--rev-3` is the specific version of `cloudci` that ran image tag `1.0.2` with a particular set of environment variables. Every change of substance — a new image, a changed env var, a scaled CPU limit — produces a new revision rather than mutating an existing one.

This three-level model maps cleanly to how a CI/CD pipeline thinks. The environment is provisioned by the platform team once; the container app is created when a service first ships; the revision is what the [pipeline](/course-book/8-devops-and-delivery/3-pipelines-as-code/) produces every time a new image moves through the build, test, and deploy stages.

## Single and multiple revision modes

Container Apps offers two policies for how revisions live alongside each other, and choosing between them is the same conversation as choosing a [deployment strategy](/course-book/8-devops-and-delivery/5-deployment-strategies/).

In **single-revision mode**, every deployment replaces the active revision: traffic moves to the new revision, the old revision is deactivated, and only one revision serves users at any time. This matches the rolling-deployment model — fast, simple, low-overhead — and is the right default when an in-place replacement is acceptable.

In **multiple-revision mode**, the previous revision keeps running after a new one is deployed, and traffic is split between them according to a configured weight (e.g., 90% to the old revision, 10% to the new one). This is the substrate for canary and blue-green deployments on Container Apps: a pipeline can publish a new revision at 0% traffic, run smoke checks against its dedicated FQDN, gradually shift traffic to it, and roll back instantly by reweighting if metrics degrade. The cost is the operational care of remembering to retire old revisions; the platform keeps up to 100 revisions per app, and stale revisions consume neither replicas nor money once their traffic weight is zero, but they do clutter the revision list.

## Ingress

Stateless services are useless without a way for traffic to reach them, and a managed platform earns its keep by making that wiring trivial.

**Ingress** (in Azure Container Apps) is the network configuration that exposes a Container App to incoming traffic; it defines the external hostname (FQDN), port, transport protocol (HTTP, gRPC), and optionally internal-only or public visibility. When ingress is enabled on an app, the platform assigns a stable FQDN of the form `cloudci.<env-id>.<region>.azurecontainerapps.io`, terminates HTTPS using a managed certificate, and routes incoming requests to whichever revision the traffic-split policy points at. The team does not configure load balancers, certificates, or DNS — the platform owns all three.

Three ingress capabilities are worth knowing about explicitly. **HTTP routing** sits at Layer 7: the platform inspects the host header and the path and chooses a revision accordingly, which is what makes weighted traffic splits possible without IP-level games. **Custom domains** (e.g., `api.cloudci.example.com`) are bound to an app by adding a CNAME to the platform-issued FQDN and uploading or letting the platform issue a certificate; from then on, the custom domain serves as the public face of the service. **mTLS** (mutual TLS) can be required on the environment level, so that every internal call between apps in the environment is authenticated and encrypted with platform-managed client certificates — useful for tightening service-to-service trust without adding sidecars.

Ingress can also be marked internal-only, in which case the FQDN is reachable only from inside the environment's virtual network. A common pattern is to expose an API-gateway app publicly and keep every backend service internal, so that the only entry point from the public internet is the gateway.

## Scale rules

The reason to use a serverless container platform rather than a fixed-size deployment is autoscaling, and Container Apps surfaces it as a small set of declarative rules.

A **scale rule** (in Azure Container Apps) is a declarative policy that automatically adjusts the number of container replicas based on a metric (CPU, memory, HTTP requests per second, custom metrics); it enables horizontal autoscaling without manual intervention. The app declares a minimum and maximum replica count and one or more rules; the platform watches the chosen metric and adjusts the replica count between those bounds.

The built-in rule kinds cover the common cases. **HTTP concurrency** scales out when the number of simultaneous in-flight HTTP requests per replica crosses a threshold, which is the right choice for request-driven web services. **CPU and memory** scale on observed resource pressure, which suits CPU-bound workers more than chatty I/O-bound services. **Custom KEDA scalers** plug in any of the dozens of triggers from the KEDA project — scale on Azure Service Bus queue depth, on Kafka consumer lag, on a cron schedule — for workloads driven by events rather than HTTP traffic. KEDA is the same autoscaler used by Kubernetes-native installations; Container Apps just exposes it without the cluster.

The minimum-replicas setting controls **scale to zero**. Setting `--min-replicas 0` lets the app shrink to no replicas when no traffic is flowing, in which case the team pays nothing for compute until a request arrives and the platform spins up a replica. The trade-off is a cold-start latency on the first request after idleness — typically a few seconds — which is fine for low-traffic internal tools and a problem for user-facing APIs with strict tail-latency requirements. Setting `--min-replicas 1` keeps one replica warm at all times.

| Scale rule | Best for | Trade-off |
|------------|----------|-----------|
| HTTP concurrency | Request-driven web APIs | Needs realistic concurrency target per replica |
| CPU / memory | CPU-bound workers, batch | Lags behind traffic spikes |
| KEDA queue length | Event-driven workers | Requires queue-length metric to be cheap to read |
| Cron (KEDA) | Predictable load schedules | Does not react to unexpected demand |

## The deployment update flow

The end-to-end flow that makes Container Apps a useful CI/CD target is short, and most pipelines come down to a single command.

```bash
az containerapp update \
  -n cloudci \
  -g rg-cloudci \
  --image mycr.azurecr.io/cloudci:1.0
```

This command updates the container app `cloudci` in resource group `rg-cloudci` to run image `mycr.azurecr.io/cloudci:1.0`. The platform does not patch the running revision; instead it creates a new immutable revision, e.g. `cloudci--rev-7`, with the new image and the existing configuration. Under single-revision mode, traffic moves to `rev-7` as soon as it reports healthy and `rev-6` is deactivated. Under multiple-revision mode, both revisions stay active and the existing traffic-split weights apply until the pipeline (or an operator) re-assigns weights.

Observing the new revision afterwards is a one-liner:

```bash
az containerapp revision list \
  -n cloudci \
  -g rg-cloudci \
  --query "[].{name:name, active:properties.active, traffic:properties.trafficWeight, image:properties.template.containers[0].image}" \
  -o table
```

The output makes the revision sequence concrete: each row is one immutable snapshot of the app, identified by its revision name and the image digest it ran. A pipeline's final smoke-test step typically reads this list to confirm that a new revision exists and is active before reporting the deployment as successful. The companion exercise [CI/CD to Azure Container Apps](/exercises/3-deployment/9-cicd-to-container-apps/) walks through this update flow end-to-end against a real Container App.

## Pulling images from ACR with managed identity

The naive way to let a Container App pull a private image is to give it a registry username and password as configuration. That is also the way that turns a CI repository into a place where ACR credentials live, where they leak when the repository is forked, and where they need to be rotated by hand. The platform-native alternative borrows the [container registry](/course-book/7-containers/6-container-registries/) chapter's identity-based authentication and removes the secret entirely.

A [managed identity](/course-book/5-identity-and-security/8-secret-management/) assigned to a container app attaches an Azure-managed principal to every replica at runtime. Granting that identity the `AcrPull` role on the registry — `az role assignment create --assignee <identity-id> --role AcrPull --scope <registry-id>` — and then pointing the app's registry configuration at the identity instead of at a username/password, lets the runtime fetch a short-lived ACR token whenever it needs to pull an image. No password is stored anywhere; rotation is handled by Azure; the same primitive is reused by other Azure services that need to read from ACR. Managed identities are the recommended default for any Container App that pulls from a private registry it controls.

## Composing with the rest of the Part

The Container Apps primitives plug into the earlier chapters of this Part with little ceremony. The [pipeline](/course-book/8-devops-and-delivery/3-pipelines-as-code/) builds and pushes the image; the [smoke gate](/course-book/8-devops-and-delivery/4-build-test-and-smoke-gates/) curls the app's FQDN after `az containerapp update` to confirm the new revision is healthy; the [deployment strategy](/course-book/8-devops-and-delivery/5-deployment-strategies/) decides whether the pipeline runs in single-revision mode (replace) or multiple-revision mode (canary or blue-green); the [pipeline-secret chapter](/course-book/8-devops-and-delivery/6-pipeline-secrets-and-oidc/) explains how the workflow itself authenticates to Azure to call `az containerapp update` without a stored secret. Container Apps does not require any of these to be in place — a developer can deploy a revision from a laptop with `az` — but the platform was designed around them, and the primitives were chosen so that a CI/CD pipeline can drive every operation through the CLI.

The result is a deployment target that occupies a deliberate middle ground: enough abstraction to disappear from the operator's day-to-day workload, enough hooks to integrate cleanly with a real pipeline, and enough flexibility to support the deployment strategies that production traffic actually demands.

## Summary

Azure Container Apps runs Docker images as a managed serverless platform, removing the operational burden of nodes, ingress controllers, and certificate rotation that a hand-rolled Kubernetes deployment carries. Its model has three layers: a Container App environment provides the network and observability boundary; a container app is the deployable service; a revision is the immutable snapshot produced every time the image or configuration changes. Single-revision mode replaces the active revision on each deploy, while multiple-revision mode keeps old revisions running so that traffic can be split for canary or blue-green strategies. Ingress wires up HTTPS with managed certificates and supports custom domains and mTLS without bespoke load-balancer configuration. Scale rules — HTTP concurrency, CPU, custom KEDA scalers — adjust replica counts automatically, and a minimum of zero replicas turns the platform into a true serverless billing model at the cost of cold-start latency. A pipeline drives all of this through `az containerapp update --image`, which creates a new revision atomically, and through a managed identity that grants the app `AcrPull` on its registry so that no credentials need to be stored anywhere.
