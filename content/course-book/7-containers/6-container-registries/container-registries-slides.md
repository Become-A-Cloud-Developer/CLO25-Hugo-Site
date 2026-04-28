+++
title = "Container Registries"
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

## Container Registries
Part VII — Containers

---

## Why a registry exists
- A built image lives in **one daemon's storage** until it is shipped somewhere
- Deployment targets — teammates, CI, production — need a **shared place** to pull from
- A **container registry** is that shared store, accessed over the OCI Distribution API
- Same protocol for Docker Hub, ACR, GHCR, and every other registry

---

## How a registry stores images
- Layers are **content-addressable blobs** — keyed by SHA256
- An **image manifest** lists the blobs that make up an image
- Repositories group versions of the same logical image
- Reference syntax: **`host/repo:tag`** (e.g. `mycr.azurecr.io/myapp:1.0`)

---

## Tags vs digests
- An **image tag** is a **mutable** pointer — `:latest` can be reassigned
- An **image digest** is a **SHA256 hash** of the manifest — immutable
- A digest changes if even one byte of any layer changes
- Production deploys should **pin to a digest**, not a tag

---

## Push and pull
- **`docker push host/repo:tag`** — uploads only the layers the registry lacks
- **`docker pull host/repo:tag`** — downloads the manifest, then missing layers
- Shared base layers transfer **once** across many images
- Resumable at the layer level — a dropped connection retries one layer

---

## Worked example — tag and push
- `docker tag myapp:local mycr.azurecr.io/myapp:1.0` adds a **second name**
- `docker push mycr.azurecr.io/myapp:1.0` streams the layers to ACR
- The tag tells the daemon **where** to push — no rebuild happens
- Any authenticated machine can now `docker pull` the same bytes

---

## Public vs private
- **Docker Hub** — default public registry; home of nginx, ubuntu, postgres
- Anonymous pulls are **rate-limited** — CI runners hit the cap on shared IPs
- **Azure Container Registry (ACR)** — private, Azure-identity-controlled
- Real systems mix both: pull base images from Hub, push apps to ACR

---

## Authenticating to the registry
- **Docker Hub** — username + **PAT** stored via `docker login`
- **ACR from a laptop** — `az acr login` exchanges Azure login for a short token
- **ACR from Azure compute** — **managed identity** with the `AcrPull` role
- **ACR from external CI** — **OIDC federation**, no stored secret

---

## Security scans
- Registries scan pushed images against public **CVE** databases
- Findings attach to a specific **digest** — the same image, scanned once
- A typical gate fails on **Critical** CVEs, warns on High and below
- Scanning raises the floor; it does not replace careful base-image choice

---

## Questions?
