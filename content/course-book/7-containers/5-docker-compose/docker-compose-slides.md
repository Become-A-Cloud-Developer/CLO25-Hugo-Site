+++
title = "Docker Compose"
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

## Docker Compose
Part VII — Containers

---

## Why Compose exists
- A real app is rarely **a single container** — web + database + queue is three
- `docker run` for each piece becomes a long, fragile sequence of commands
- Order, flags, and hostnames have to match across every developer machine
- A second developer joining the project should not need that incantation
- **Compose** collapses it into one file checked into the repository

---

## What Compose is
- **Docker Compose** = a tool that reads `docker-compose.yml` and orchestrates multi-container apps
- Targets the **local development** workflow, not production
- Automates networking, volume management, and environment setup
- One command — `docker compose up` — starts the whole stack
- Same file drives integration tests in CI

---

## The compose file
- A YAML document, conventionally `compose.yaml` at the repo root
- Three top-level keys that matter: `services`, `volumes`, `networks`
- `services` is mandatory; the rest are optional
- Compose creates a **default network** for the project automatically
- Modern Compose ignores the old `version:` key — leave it out

---

## Services
- A **service** = a named, containerized application
- Each service entry under `services:` becomes one container at runtime
- The service name doubles as the **DNS hostname** inside the project network
- Either `build: .` (build from local Dockerfile) or `image: mongo:7` (pull from registry)
- `ports: ["8080:8080"]` publishes to the host; service-to-service traffic does not need it

---

## Named networks and DNS
- A **named network** is created and managed by Compose
- Services on the same network resolve each other by **service name**
- Compose runs an embedded DNS server inside the project network
- The web container reaches the database at `mongodb://db:27017` — no `localhost`, no IP
- New container, new IP — DNS lookup returns the new address

---

## Volumes
- Containers are **ephemeral** — `docker compose down` discards their writable layer
- A **named volume** is a directory the Docker daemon manages on the host
- Survives container removal; reattaches on the next `docker compose up`
- `volumes: { db-data: {} }` declares it; `volumes: [db-data:/data/db]` mounts it
- Bind mounts are for source code, not for database state

---

## Environment variables and .env
- Containerized apps read configuration from **environment variables**
- `environment:` sets variables inside one service
- `.env` file is read automatically; values interpolated as `${VAR}` in the compose file
- `.env` is gitignored — secrets stay out of the repo
- `MongoDB__ConnectionString=mongodb://db:27017` is the .NET pattern

---

## Dependency ordering
- **`depends_on`** declares one service should start after another
- Does not guarantee the dependency is **ready** — only that it has started
- Pair with a `healthcheck` and `condition: service_healthy` for readiness
- The healthcheck runs a probe inside the dependency container
- Production-grade fix is **retry logic** in the application

---

## Worked example: web + db
- `web` service built from local Dockerfile, port 8080 published
- `db` service is `mongo:7`, volume `db-data` mounted at `/data/db`
- Both services on named network `appnet`
- `web` waits for `db` healthcheck (`mongosh --eval ping`) to pass
- `MongoDB__ConnectionString=mongodb://db:27017` resolves via Compose DNS

---

## Compose vs production
- Compose runs on **a single host** — no horizontal scaling, no self-healing
- **Azure Container Apps** for managed multi-host production
- **Kubernetes** for large-scale or complex topologies
- Service / volume / env-var concepts translate forward — Part VIII develops them
- Compose is local-dev; production is a different runtime

---

## Questions?
