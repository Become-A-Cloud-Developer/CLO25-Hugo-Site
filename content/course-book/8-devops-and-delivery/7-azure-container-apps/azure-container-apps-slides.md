+++
title = "Azure Container Apps as a Deployment Target"
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

## Azure Container Apps as a Deployment Target
Part VIII — DevOps and Delivery

---

## Why a managed runtime
- Stateless services need **scaling, ingress, certificates** — but not a full cluster
- Plain VMs leave patching and load-balancing to the team
- A full **Kubernetes** cluster adds nodes, RBAC, and ingress controllers to run
- **Azure Container Apps** sits in the gap — managed, serverless, opinionated

---

## The Container Apps model
- A **Container App environment** is the network and observability boundary
- A **container app** is the deployable unit — image, env vars, ingress, scale
- A **revision** is an immutable snapshot of one app's image and configuration
- Every change of substance produces a **new revision** — never an in-place edit

---

## Single vs multiple revision mode
- **Single-revision** mode replaces the active revision on every deploy
- **Multiple-revision** mode keeps old revisions running for **traffic splitting**
- Multiple-revision is the substrate for **canary** and **blue-green** strategies
- Stale revisions cost nothing at zero traffic, but clutter the revision list

---

## Ingress
- **Ingress** wires up the public **FQDN**, HTTPS, and Layer-7 routing
- Managed certificates — no manual cert lifecycle
- **Custom domains** via CNAME plus platform-issued or uploaded certs
- **mTLS** on the environment authenticates service-to-service calls

---

## Scale rules
- **HTTP concurrency** — scale on in-flight requests per replica
- **CPU / memory** — scale on observed resource pressure
- **Custom KEDA scalers** — Service Bus queue depth, Kafka lag, cron
- **Scale to zero** with `--min-replicas 0` — pay nothing while idle

---

## The deployment update flow
- `az containerapp update --image mycr.azurecr.io/cloudci:1.0` creates a new revision
- The platform never patches a running revision — always a **new immutable snapshot**
- `az containerapp revision list` shows the revision history per app
- A pipeline's smoke gate confirms the new revision is **active** and healthy

---

## Pulling from ACR with managed identity
- Naive: store registry username + password as app config
- Native: assign a **managed identity** to the app, grant it `AcrPull` on the registry
- The runtime mints a **short-lived ACR token** every pull — no stored secret
- Same primitive other Azure services use to read from ACR

---

## Composing with the rest of the Part
- The **pipeline** builds and pushes the image
- The **smoke gate** curls the FQDN after `az containerapp update`
- The **deployment strategy** picks single- or multiple-revision mode
- **OIDC federation** lets the pipeline call `az containerapp update` with no secret

---

## Questions?
