+++
title = "Docker Compose"
program = "CLO"
cohort = "25"
courses = ["ACD"]
weight = 50
date = 2026-04-28
draft = false
+++

[Watch the presentation](/presentations/course-book/7-containers/5-docker-compose.html)

[Se presentationen på svenska](/presentations/course-book/7-containers/5-docker-compose-swe.html)

---

A real application is rarely a single container. A web application talks to a database, the database persists data to disk, and a queue or cache often sits between them. Running each piece by hand with `docker run` quickly becomes a sequence of long, fragile commands: a network has to be created first, the database needs a volume mount, the application needs the right environment variables and the correct hostname for the database. Compose collapses all of that into one declarative file. The sections below develop the compose model — services, named networks, volumes, environment variables, and dependency ordering — and end with a worked example that mirrors the companion exercise at [/exercises/20-docker/](/exercises/20-docker/).

## The problem Compose solves

A web application that uses MongoDB needs three things to run end-to-end on a developer laptop: the application process, the database process, and a network between them. Started by hand, the sequence is something like:

```bash
docker network create appnet
docker volume create db-data
docker run -d --name db --network appnet -v db-data:/data/db mongo:7
docker run -d --name web --network appnet -p 8080:8080 \
  -e MongoDB__ConnectionString=mongodb://db:27017 myapp:dev
```

Five commands, in the right order, with the right flags, every time the laptop reboots. A second developer joining the project has to learn that exact incantation, and any drift between developers becomes a "works on my machine" bug. **Docker Compose** is a tool that reads a `docker-compose.yml` file and orchestrates multi-container applications; it automates networking, volume management, and environment setup for local development and testing. The five commands above collapse into one file checked into the repository and one command (`docker compose up`) that any developer can run.

Compose targets the local development workflow. The same file can also drive integration tests in a CI pipeline and one-off staging environments, but Compose is not a production orchestrator. Production scaling, rolling updates, and self-healing belong to platforms covered later in this Course Book — Azure Container Apps and Kubernetes are previewed at the end of this chapter and developed in Part VIII.

## The compose file

A **compose file** (docker-compose.yml) is a YAML document that declares services, volumes, networks, and environment variables for a multi-container application; it serves as the source of truth for the application's topology. The file lives at the root of the repository, alongside the `Dockerfile` for the application. Compose accepts both `docker-compose.yml` and `compose.yaml` as the default name; the latter is the form recommended by the current Compose specification.

The file has four top-level keys that matter for most projects:

```yaml
services:   # one entry per container
volumes:    # named volumes that outlive containers
networks:   # named networks the services share
```

`services` is mandatory; `volumes` and `networks` are optional. When `networks` is omitted, Compose creates a default network for the project automatically — every service on it can reach every other service by service name. That implicit network is the most important convenience Compose provides, and the worked example below leans on it.

A fourth top-level key, `version`, used to be required. Current Compose releases ignore it, and recent documentation removes it entirely. New files should not include a `version` line.

## Services

A **service** (in Compose context) is a named, containerized application; multiple services communicate via a Docker Compose-managed network, each addressable by its service name as a hostname (e.g., the `web` service reaches `mongodb` at hostname `mongodb`). Each service entry under `services:` becomes one container when Compose runs the file. The service name is the key in the YAML map and doubles as the DNS hostname inside the project network.

A service has either a `build:` directive (Compose builds an image from a local Dockerfile) or an `image:` directive (Compose pulls a pre-built image from a registry), but not both meaningfully — `build` produces an image that `image` can name. The minimum useful service definition specifies one of those two, plus any ports, environment variables, or volume mounts the container needs:

```yaml
services:
  web:
    build: .
    ports:
      - "8080:8080"
  db:
    image: mongo:7
```

Two services, one built locally from the Dockerfile in the current directory, the other pulled from Docker Hub. Compose places both on the project's default network and assigns them DNS names `web` and `db`. Inside the `web` container, the hostname `db` resolves to the database container's IP — no `localhost`, no hardcoded address, no manual `--link`.

### Ports and exposure

`ports` publishes a container port to the host. The form `"8080:8080"` maps host port 8080 to container port 8080, making the service reachable from a browser at `http://localhost:8080`. Service-to-service traffic does not need `ports` — services on the same network can talk to each other on any port the target container listens on, regardless of whether that port is published to the host. The database in the example above is reachable from the web container at `db:27017` even though no `ports` entry exposes 27017 to the laptop.

This distinction matters for security. Publishing the database port to the host is convenient for connecting a GUI client, but it also opens the database to anything else running on the developer machine. Leaving it unpublished keeps the database reachable only from the project network.

## Named networks

A **named network** is a Docker network created and managed by Compose; services connected to the same named network can resolve each other by hostname via embedded DNS, enabling service-to-service communication without hardcoded IPs. Compose creates one named network per project automatically — its name is `<project>_default` where `<project>` is the directory name of the compose file. Every service joins that default network unless told otherwise.

For most projects, the default network is enough. Larger compositions sometimes split services across multiple networks to enforce isolation: a `frontend` network where the web service is reachable from the host, and a `backend` network where only the web and database services live. A compose file can declare networks explicitly:

```yaml
services:
  web:
    networks: [appnet]
  db:
    networks: [appnet]

networks:
  appnet:
```

Naming the network gives it a stable, predictable name (`<project>_appnet`) and documents the topology in the file. Beyond that, behavior is identical to the implicit default network.

### DNS-based service discovery

Compose runs an embedded DNS server inside the project network. When the web container resolves the hostname `db`, the DNS server returns the database container's current IP address. If the database container is restarted and gets a new IP, the next DNS lookup returns the new address — service discovery survives container churn without any application change. This is what makes a connection string of the form `mongodb://db:27017` work without modification across every developer machine and every CI run.

## Volumes

A **volume** in Docker is a directory managed by the Docker daemon (or host-mounted) that persists data beyond a container's lifetime; in Compose, `volumes: { data: {} }` declares a named volume and `service: volumes: [data:/path]` mounts it into the container. Containers are designed to be ephemeral — `docker compose down` removes them and any data written inside the container's writable layer disappears with them. A database container without a volume loses every record on shutdown.

A named volume is a directory the Docker daemon manages on the host filesystem. It survives container removal, can be reattached to a new container at the same path, and is portable across container restarts. In Compose, declaring and mounting a named volume takes two lines:

```yaml
services:
  db:
    image: mongo:7
    volumes:
      - db-data:/data/db

volumes:
  db-data:
```

The top-level `volumes:` key declares the volume; the service's `volumes:` list mounts it at `/data/db` inside the container, which is where MongoDB writes its data files. Bringing the stack down with `docker compose down` removes the containers but leaves the volume intact. Bringing it back up with `docker compose up` reattaches the volume and the database starts with the previous data still present. Removing the volume requires the explicit `docker compose down -v` flag.

Bind mounts are a different form: they map a host directory into the container directly. Bind mounts are appropriate during development for source code (so file edits on the host are visible inside the container) but inappropriate for database state, because they spread filesystem semantics and permissions across host and container in ways that can corrupt data files.

## Environment variables and .env files

Containerized applications read configuration from [environment variables](/course-book/3-application-development/5-configuration-and-environments/) rather than baked-in config files, which keeps the same image deployable across development, staging, and production. Compose supports environment variables in two complementary ways.

The `environment:` key on a service sets variables inside that container:

```yaml
services:
  web:
    environment:
      - MongoDB__ConnectionString=mongodb://db:27017
      - ASPNETCORE_ENVIRONMENT=Development
```

The double-underscore syntax (`MongoDB__ConnectionString`) is the .NET configuration convention for nesting — the application reads this as `MongoDB:ConnectionString` in its configuration tree. The value is a [connection string](/course-book/4-data-access/3-connections-and-transactions/) that uses the service hostname `db`, not `localhost`.

A `.env` file in the same directory as the compose file is read automatically; its values are interpolated into the compose file using `${VAR}` syntax:

```yaml
services:
  db:
    image: mongo:7
    environment:
      - MONGO_INITDB_ROOT_USERNAME=${DB_USER}
      - MONGO_INITDB_ROOT_PASSWORD=${DB_PASSWORD}
```

The `.env` file is conventionally gitignored, so secrets stay out of the repository while a committed `.env.example` documents the expected variables. Compose substitutes the values at `up` time; the running container only sees the resolved environment.

## Dependency ordering

A **dependency** in Compose (specified via `depends_on`) declares that one service should start after another; however, it does not guarantee the dependency is ready, so production-grade services use retry logic or readiness checks instead. A web service that connects to a database at startup needs the database container to exist first, but `depends_on` only guarantees the database container has been started — not that the database process inside it has finished initializing and is accepting connections.

The fully specified form combines `depends_on` with a `healthcheck` on the dependency:

```yaml
services:
  db:
    image: mongo:7
    healthcheck:
      test: ["CMD", "mongosh", "--eval", "db.runCommand({ping:1})"]
      interval: 5s
      timeout: 3s
      retries: 10
  web:
    build: .
    depends_on:
      db:
        condition: service_healthy
```

With `condition: service_healthy`, Compose holds the web container's start until the database's healthcheck reports healthy. The healthcheck runs `mongosh` inside the database container every five seconds and counts the database as healthy once the ping command succeeds. This is the closest Compose comes to true readiness ordering.

Even with healthchecks, applications should retry their initial database connection. A network glitch, a long-running migration, or a transient lock can fail the first connection attempt long after the container reports healthy. Retry logic in the application code (with backoff, capped at a sensible number of attempts) is the production-grade fix; `depends_on` is the development-time convenience.

## Worked example: web and database

The companion exercise at [/exercises/20-docker/4-docker-compose-local-development-stack/](/exercises/20-docker/4-docker-compose-local-development-stack/) builds a complete `compose.yaml` for an ASP.NET Core application backed by MongoDB. A condensed version captures the load-bearing pieces:

```yaml
services:
  web:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - MongoDB__ConnectionString=mongodb://db:27017
      - FeatureFlags__UseMongoDB=true
    depends_on:
      db:
        condition: service_healthy
    networks:
      - appnet

  db:
    image: mongo:7
    volumes:
      - db-data:/data/db
    healthcheck:
      test: ["CMD", "mongosh", "--eval", "db.runCommand({ping:1})"]
      interval: 5s
      timeout: 3s
      retries: 10
    networks:
      - appnet

volumes:
  db-data:

networks:
  appnet:
```

Reading top-down, the file declares two services on a shared named network `appnet`. The `web` service builds from the local Dockerfile, publishes port 8080 to the host, and reads its database connection string from an environment variable that uses the hostname `db` — Compose's embedded DNS resolves that hostname to the database container's IP. The `web` service waits for the `db` healthcheck to pass before starting. The `db` service mounts a named volume `db-data` at MongoDB's data directory, so records persist across `docker compose down` and `docker compose up` cycles. A feature flag `FeatureFlags__UseMongoDB=true` toggles the application's use of MongoDB without code change, demonstrating how environment variables drive runtime behavior.

Running `docker compose up --build` from the repository root builds the web image, pulls the MongoDB image, creates the `appnet` network and `db-data` volume, starts both containers in dependency order, and streams their logs to the terminal. `docker compose down` stops and removes the containers and the network but leaves the volume; `docker compose down -v` removes the volume too.

## Compose and production orchestration

Compose is a local-development tool. It runs containers on a single host (the developer laptop or a CI runner) with no horizontal scaling, no rolling updates, and no self-healing across multiple machines. Production environments need more.

| Tool | Scope | Use case |
|------|-------|----------|
| Docker Compose | Single host | Local dev, integration tests, simple staging |
| Azure Container Apps | Managed multi-host | Production for most web/API workloads |
| Kubernetes | Multi-host cluster | Production at scale, complex topologies |

The compose model — services, networks, volumes, environment variables, dependency ordering — translates conceptually to both production targets, even though the file format and runtime are different. A service in Compose maps to a Container App in Azure Container Apps and to a Deployment + Service in Kubernetes. A named volume maps to managed storage. The environment variables move into a configuration store or secret manager. Part VIII of this Course Book develops the production side of this story; the skills built here in Compose carry over directly.

## Summary

Docker Compose is the local-development orchestrator for multi-container applications: a single YAML file declares services, networks, volumes, and environment variables, and a single command brings them all up. Each service becomes a container on a Compose-managed network where service names resolve as hostnames via embedded DNS, eliminating hardcoded addresses. Named volumes persist state that must outlive containers, while environment variables and `.env` files drive configuration without rebuilding images. `depends_on` with a `healthcheck` orders startup, but applications should still retry their initial connections. The compose file is checked into the repository, becomes the source of truth for the application's local topology, and forward-translates conceptually to Azure Container Apps and Kubernetes for production.
