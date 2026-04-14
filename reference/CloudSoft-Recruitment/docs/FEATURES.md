# CloudSoft Recruitment Portal — Feature Overview

This is a **job recruitment portal** built as an ASP.NET MVC monolith. It has two user roles — **Admin** and **Candidate** — with distinct capabilities.

## Public (No Login)

- Browse all job listings in a card grid
- View full job details (title, location, deadline, description)

## Candidate Features

- **Login** via email/password or **Google OAuth** (auto-creates account)
- **Apply for jobs** with a cover letter and optional PDF CV upload
- **View own applications** in a dashboard ("My Applications")
- Duplicate application prevention (one application per job)

## Admin Features

- **Login** via email/password (cookie authentication, account seeded on startup)
- **Create, edit, and delete** job postings (title, description, location, deadline)
- **View all applications** received for any job
- **Download candidate CVs**

## CV Upload Security

Uploads are validated by file extension, content type, magic bytes (`%PDF-`), and a 5 MB size limit. Files are stored with GUID filenames in either Azure Blob Storage or local disk (fallback).

## Authentication & Security

- ASP.NET Core Identity with account lockout (5 attempts → 5 min lock)
- **Identity storage**: SQLite database (Docker volume locally, ephemeral container filesystem on ACA — recreated on startup via `Migrate()` + seed). Chosen over Azure SQL serverless due to its ~1 minute cold-start time, which would require complex retry logic. Works well for single-replica deployments; managed databases are introduced in the next course.
- CSRF protection globally, over-posting prevention via `[Bind]`
- Google OAuth conditionally enabled (hidden when not configured)
- Admin password stored in User Secrets, never in config files

## Runtime Scenarios

The application supports four runtime scenarios, each adding a layer of complexity:

### 1. In-Memory Local (`dotnet run`)
Zero external dependencies. Uses in-memory repositories, local disk blob storage, and SQLite for Identity. Fastest iteration — no Docker needed.

### 2. Docker Compose (`docker compose up`)
External dependencies run as containers: MongoDB for data, Azurite for blob storage. The app also runs in Docker. Tests real database and blob storage behavior locally without installing anything on the host.

### 3. Azure with In-Memory
The app deployed to Azure Container Apps via the CI/CD pipeline, but with `FeatureFlags:UseMongoDB=false` and no blob connection string. Verifies that the deployment pipeline, container image, health probes, and Key Vault integration all work — without needing CosmosDB or Blob Storage provisioned. Uses the same graceful degradation built into the code.

### 4. Full Azure Production
All services on Azure: CosmosDB (MongoDB API) for data, Blob Storage for CVs, Key Vault for secrets and feature flags, Application Insights for telemetry. SQLite Identity is ephemeral (recreated on startup via `Migrate()` + seed). The production configuration.

Each scenario builds on the previous one, and the code's graceful degradation (feature flags, conditional service registration) means the same Docker image works across all four.

## API Integration

### Consuming: REST Countries

The Create Job form uses the [REST Countries API](https://restcountries.com) to validate or suggest locations. Teaches `HttpClient`, DI registration, async external calls, and graceful fallback when the API is unavailable.

### Providing: Jobs API

The application exposes its job listings as a REST API, enabling external consumers (e.g., partner job boards, mobile apps) to access the same data served by the MVC views:

- `GET /api/jobs` — list all jobs (public)
- `GET /api/jobs/{id}` — single job details (public)
- `POST /api/jobs` — create a job (Admin)
- `GET /api/jobs/{id}/applications` — applications for a job (Admin)

The API reuses the existing service layer, demonstrating how one business logic layer can power multiple interfaces.

### DTOs (Data Transfer Objects)

The API introduces a clear separation between internal models and external contracts:

- **Domain/DB models** (e.g., `Job`) — carry BSON attributes and internal fields like `PostedByUserId`
- **API request DTOs** (e.g., `CreateJobRequest`) — only the fields a caller should submit
- **API response DTOs** (e.g., `JobResponse`) — only the fields a caller should see, excluding internal details

The MVC views continue using the domain models directly. DTOs are added only at the API boundary, demonstrating separation of concerns, security (no leaking of internal data), and independent versioning of the API contract.

## Logging and Monitoring

### Application Insights (Azure Monitor)

The application uses Azure Application Insights (free tier: 5 GB/month) for telemetry and log analysis. Adding the `Microsoft.ApplicationInsights.AspNetCore` package and a connection string enables automatic capture of request telemetry, exceptions, dependencies, and response times.

### Structured Logging

ASP.NET Core's built-in `ILogger` supports structured logging natively — log message placeholders (e.g., `{UserId}`, `{IpAddress}`) become queryable custom dimensions in Application Insights. No third-party libraries (Serilog, etc.) are required.

Key events logged with structured properties:

- **Authentication** — successful logins (user, method, IP address), failed login attempts (email, IP), logouts
- **Business activity** — job created/edited/deleted (by which admin), application submitted (candidate, job), CV downloaded (which admin, whose CV)
- **External calls** — REST Countries API response time and success/failure
- **Operational** — blob storage upload time and file size

### Querying with KQL

Application Insights uses Kusto Query Language (KQL) in the Azure Portal. Structured log properties enable targeted queries such as:

- Find all logins by a specific user
- Summarize failed login attempts by IP address
- Identify the most-applied-to jobs
- Track REST Countries API reliability
- Monitor response times per endpoint

KQL is used across the Azure ecosystem (Monitor, Sentinel, Defender), making it a transferable skill.

### Local Development

Structured logs appear in the console in a readable format during local development. Application Insights is only needed for cloud-scale querying and dashboards — the logging code works the same in all environments.

## Health Endpoints and Version Reporting

### Health Checks

ASP.NET Core's built-in health check framework (`Microsoft.Extensions.Diagnostics.HealthChecks`) exposes a `/health` endpoint that reports the status of the application and its dependencies (MongoDB connectivity, blob storage availability, etc.). Azure Container Apps uses liveness, readiness, and startup probes against this endpoint to decide whether to route traffic to a container or restart it.

### Application Version

The application embeds its git commit hash at build time via `InformationalVersion`:

```bash
dotnet publish /p:InformationalVersion="1.0.0+a1b2c3d"
```

At runtime, the app reads its own version and exposes it via the health or a `/version` endpoint. This enables anyone to check exactly which commit is running in any environment.

### End-to-End Traceability

The git commit hash flows through every layer, creating full traceability:

**Jira ticket → branch/commits → PR → Docker image tag → assembly version → health endpoint**

Docker images are tagged with the short git commit hash (e.g., `cloudsoft-web:a1b2c3d`) in the CI/CD pipeline. Combined with Jira ticket keys in branch names and commit messages (e.g., `CLO-42: Add structured logging`), this enables tracing from a running container all the way back to the original work item.

## Project Management (Jira)

Jira is used as the project management tool for tracking work. Epics, stories, and tasks are structured to mirror the development workflow. The key value is the traceability chain: Jira ticket keys referenced in branch names, commit messages, and PRs create an auditable link from requirement to deployed code. Jira is chosen as a widely used industry tool in this region.

## Deployment Progression

The application uses Docker as the single packaging format across all stages:

1. **Local (Docker Compose)** — The app, MongoDB, and Azurite run together as containers on the developer's machine
2. **Manual Azure deployment** — Docker image is pushed to public Docker Hub, then pulled into Azure Container Apps
3. **CI/CD** — GitHub Actions builds and pushes the image to Azure Container Registry (ACR) using RBAC, then deploys to Container Apps
4. **Production-hardened** — Azure Key Vault is added for secrets management, referenced directly from within the running container

## Planned (Weeks 4–8)

The monolith is designed to evolve into microservices: BFF pattern, async messaging for notifications, CI/CD pipelines, container scanning, Bicep IaC, and Application Insights.
