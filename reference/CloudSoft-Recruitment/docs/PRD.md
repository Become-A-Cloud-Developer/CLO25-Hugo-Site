# Product Requirements Document — CloudSoft Recruitment Portal

**Version:** 1.0
**Date:** 2026-04-07
**Status:** Implemented

## 1. Overview

CloudSoft Recruitment Portal is a job recruitment web application where administrators post job listings and candidates apply with cover letters and CVs. The application serves as the reference implementation for the Advanced Cloud Development (ACD) course in the CLO25 program.

The portal is built as a .NET 10 MVC monolith with a REST API layer, designed to run identically across four runtime scenarios — from a zero-dependency local setup to a fully managed Azure production deployment — using feature flags and configuration layering.

## 2. Users and Roles

### 2.1 Public (Unauthenticated)
- Browse all job listings in a card grid layout
- View full details of any job (title, description, location, deadline, posted by)
- Access the Jobs REST API for public endpoints

### 2.2 Candidate
- Log in via email/password (cookie authentication)
- Log in via Google OAuth (auto-creates account on first login)
- Apply for a job with a cover letter and an optional PDF CV upload
- View own submitted applications in a dashboard ("My Applications")
- One application per job (duplicate prevention enforced server-side)

### 2.3 Admin
- Log in via email/password (account seeded at application startup)
- Create, edit, and delete job postings (title, description, location, deadline)
- View all applications received for any job
- Download candidate CVs
- Create jobs via REST API using JWT bearer token or API key

## 3. Functional Requirements

### 3.1 Job Management
- **Create:** Admin submits title (max 100 chars), description (max 5000 chars), location (max 100 chars), and deadline (must be a future date). System records `PostedAt`, `PostedByUserId`, and `PostedByName` automatically.
- **Edit:** Admin can update title, description, location, and deadline. System-set fields (`PostedAt`, `PostedByUserId`, `PostedByName`) are preserved.
- **Delete:** Admin confirms deletion via a confirmation page.
- **List:** Public card grid showing title, location, truncated description, and deadline.
- **Details:** Full job view with role-conditional action buttons (Apply for candidates, Edit/Delete/View Applications for admins).

### 3.2 Application Submission
- Candidate selects a job and submits a cover letter (required, max 5000 chars) and optional PDF CV.
- System denormalizes job title, candidate email, candidate name, and candidate ID into the application document.
- System sets `AppliedAt` timestamp server-side.
- Duplicate applications for the same job by the same candidate are rejected.
- CV is uploaded to blob storage (Azure Blob Storage or local disk fallback) with a GUID-based filename.

### 3.3 CV Upload Security
All uploaded files are validated with defense-in-depth:
- File extension must be `.pdf`
- Content-Type header must be `application/pdf`
- First 5 bytes must match PDF magic bytes (`%PDF-`)
- Maximum file size: 5 MB
- Filename replaced with server-generated GUID
- Blob container access: private (no public URLs)

### 3.4 REST API
The application exposes a JSON REST API at `/api/jobs` reusing the same service layer as the MVC controllers:

| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/jobs` | GET | Public | List all jobs |
| `/api/jobs/{id}` | GET | Public | Get job by ID |
| `/api/jobs` | POST | Admin (JWT/API key/cookie) | Create a job |
| `/api/jobs/{id}/applications` | GET | Admin (JWT/API key/cookie) | List applications for a job |
| `/api/token` | POST | None (email/password in body) | Issue a JWT bearer token |

API requests and responses use dedicated DTOs (`CreateJobRequest`, `JobResponse`, `ApplicationResponse`, `TokenRequest`, `TokenResponse`), separate from the domain models.

### 3.5 REST Countries Integration
The application consumes the [REST Countries API](https://restcountries.com) for location validation and suggestions. The service degrades gracefully when the API is unavailable (returns empty results, does not block job creation). Controlled by the `UseRestCountries` feature flag.

### 3.6 Health and Version Endpoints
- `GET /health` — returns JSON health status with dependency check results. Used by Azure Container Apps liveness, readiness, and startup probes.
- `GET /version` — returns the application version including the git commit hash embedded at build time via `AssemblyInformationalVersion`.

## 4. Authentication and Authorization

### 4.1 MVC Authentication (Browser)
- **Cookie authentication** via ASP.NET Core Identity for both Admin and Candidate roles
- **Google OAuth 2.0** authorization code flow for Candidate users (conditionally enabled via feature flag and credentials)
- Account lockout: 5 failed attempts triggers a 5-minute lockout
- Cookies: HttpOnly, SameSite=Lax, SecurePolicy varies by environment
- CSRF protection via global `AutoValidateAntiforgeryTokenAttribute`
- Over-posting prevention via `[Bind]` attributes on form models

### 4.2 API Authentication
- **JWT bearer tokens**: Issued via `POST /api/token` with email/password credentials. Tokens are signed with HS256 using a key stored in Key Vault (production) or appsettings (development). Contains claims for user ID, email, role, and name.
- **API key**: Static key sent via `X-API-Key` header, validated by middleware. Grants Admin-level access. Key stored in Key Vault (production) or configuration (development).
- Both schemes work alongside cookie authentication. API controllers accept `Bearer` and `Identity.Application` schemes.

### 4.3 Infrastructure Authentication (Azure)
- **GitHub Actions → Azure Container Registry**: Service principal credentials stored as GitHub secrets
- **Container Apps → CosmosDB, Blob Storage, Key Vault**: System-assigned managed identity with RBAC role assignments (Cosmos DB Data Contributor, Storage Blob Data Contributor, Key Vault Secrets User)
- **Container Apps → Azure Files**: Storage account key managed by the ACA platform
- **Container Apps → Application Insights**: Connection string via environment variable

## 5. Data Architecture

### 5.1 Domain Data (Jobs, Applications)
- Stored in MongoDB (local/Docker) or Azure CosmosDB with MongoDB API (production)
- Fallback: in-memory repositories using `ConcurrentDictionary` when `UseMongoDB` is false
- Collections: `jobs`, `applications` with BSON serialization attributes
- Application documents are denormalized (contain job title, candidate email/name for fast reads)

### 5.2 Identity Data (Users, Roles)
- Stored in SQLite via Entity Framework Core
- Persisted via Docker volume (local) or Azure Files volume mount (Azure)
- Chosen over Azure SQL serverless due to ~1 minute cold-start latency that would require complex retry logic
- Suitable for single-replica deployments; managed databases deferred to next course

### 5.3 File Storage (CVs)
- Stored in Azure Blob Storage (production) or Azurite emulator (Docker Compose) or local disk (in-memory scenario)
- Private blob container with no public access
- Files accessed exclusively through the application (admin download endpoint)

### 5.4 Secrets
- **Development**: .NET User Secrets (never in appsettings.json)
- **Docker Compose**: Environment variables in docker-compose.yml
- **Production**: Azure Key Vault via managed identity and `DefaultAzureCredential`
- Secret naming convention: `AdminSeed--Password` in Key Vault maps to `AdminSeed:Password` in .NET configuration

## 6. Runtime Scenarios

The same Docker image supports four runtime scenarios controlled by feature flags and configuration layering:

### 6.1 In-Memory Local (`dotnet run`)
- **Data**: In-memory repositories (ConcurrentDictionary)
- **Blobs**: Local disk (`data/uploads/`)
- **Identity**: SQLite file (`identity.db`)
- **Secrets**: User Secrets
- **Telemetry**: Console output
- **Config**: `appsettings.json` → `appsettings.Development.json` → User Secrets

### 6.2 Docker Compose (`docker compose up`)
- **Data**: MongoDB container
- **Blobs**: Azurite container (Azure Storage emulator)
- **Identity**: SQLite in Docker volume
- **Secrets**: Environment variables in docker-compose.yml
- **Telemetry**: Console output
- **Config**: `appsettings.json` → `appsettings.Development.json` → Docker env vars

### 6.3 Azure with In-Memory (Staging)
- **Data**: In-memory repositories
- **Blobs**: Local disk inside container
- **Identity**: SQLite on Azure Files volume mount
- **Secrets**: Azure Key Vault
- **Telemetry**: Application Insights
- **Config**: `appsettings.json` → `appsettings.Staging.json` → ACA env vars → Key Vault
- **Purpose**: Verify deployment pipeline without provisioning CosmosDB/Blob Storage

### 6.4 Full Azure Production
- **Data**: Azure CosmosDB (MongoDB API, serverless)
- **Blobs**: Azure Blob Storage (private container)
- **Identity**: SQLite on Azure Files volume mount
- **Secrets**: Azure Key Vault
- **Telemetry**: Application Insights
- **Config**: `appsettings.json` → `appsettings.Production.json` → ACA env vars → Key Vault

### 6.5 Feature Flags

| Flag | Default | Effect when off | Effect when on |
|---|---|---|---|
| `UseMongoDB` | false | In-memory repositories | MongoDB / CosmosDB repositories |
| `UseBlobStorage` | false | LocalBlobService (disk) | Azure BlobService |
| `UseGoogleAuth` | false | Google login button hidden | Google OAuth active |
| `UseKeyVault` | false | Local configuration only | Azure Key Vault loaded |
| `UseApplicationInsights` | false | Console logging only | App Insights telemetry |
| `UseRestCountries` | true | DisabledCountryService | REST Countries API calls |

## 7. Logging and Monitoring

### 7.1 Structured Logging
All controllers and services use ASP.NET Core's built-in `ILogger<T>` with structured message templates. Key events logged with queryable properties:
- **Authentication**: login success/failure (email, IP), lockout, Google OAuth, logout
- **Business activity**: job created/edited/deleted (admin ID, job title), application submitted (candidate ID, job ID), CV downloaded
- **External calls**: REST Countries API response status
- **Operational**: blob upload, repository type in use

### 7.2 Application Insights
When enabled, the `Microsoft.ApplicationInsights.AspNetCore` SDK captures:
- Request telemetry (response times, status codes)
- Dependency tracking (database calls, HTTP calls)
- Exception telemetry
- Custom dimensions from structured log properties

Queryable via Kusto Query Language (KQL) in the Azure Portal.

### 7.3 Startup Diagnostics
The application logs which features are enabled/disabled at startup for each runtime scenario.

## 8. Infrastructure

### 8.1 Azure Resources (Bicep)
Provisioned via 9 Bicep modules orchestrated by `infra/bicep/main.bicep`:

| Module | Resource | SKU / Tier |
|---|---|---|
| `container-registry.bicep` | Azure Container Registry | Basic |
| `managed-identity.bicep` | User-assigned managed identity | — |
| `key-vault.bicep` | Azure Key Vault | Standard, RBAC, 7-day soft delete |
| `cosmos-db.bicep` | CosmosDB (MongoDB API 7.0) | Serverless |
| `log-analytics.bicep` | Log Analytics workspace | PerGB2018, 30-day retention |
| `application-insights.bicep` | Application Insights | Web, linked to Log Analytics |
| `storage-account.bicep` | Storage Account | Standard_LRS, HTTPS only, TLS 1.2 |
| `container-apps-env.bicep` | Container Apps environment | Linked to Log Analytics |
| `container-app.bicep` | Container App | 0.5 CPU, 1Gi RAM, 1–3 replicas |
| `key-vault-secrets.bicep` | Key Vault secrets | Connection strings + feature flags |

### 8.2 Container App Configuration
- **Ingress**: External HTTPS, port 8080
- **Scaling**: HTTP-based (50 concurrent requests), 1–3 replicas
- **Revisions**: Single revision mode (prevents concurrent containers competing for resources)
- **Identity**: SQLite on container local filesystem (ephemeral, recreated on startup via `Migrate()` + seed)
- **Health probes**: Startup (160s budget), liveness (30s interval), readiness (10s interval) — all on `/health`

### 8.3 Docker
- **Dockerfile**: Multi-stage build (SDK → runtime), `ARG VERSION` for commit hash embedding, non-root user, exposes port 8080
- **Docker Compose**: MongoDB + Azurite + app, with named volumes for data persistence, feature flags as environment variables

## 9. CI/CD Pipeline

### 9.1 GitHub Actions Workflow (`.github/workflows/ci-cd.yml`)
Three sequential jobs triggered on push to `main`:

1. **build-and-test**: Restore, build, run unit/integration tests
2. **docker-build-push**: Build Docker image with commit hash version, push to Docker Hub (tagged with full SHA and `latest`)
3. **deploy**: Login to Azure, update Container App image, verify health endpoint

### 9.2 Traceability
Git commit hash flows through every layer:
```
Jira ticket → Git branch → Commits → Pull Request → Docker image tag → AssemblyInformationalVersion → GET /version
```

### 9.3 Required Secrets
| Secret | Purpose |
|---|---|
| `DOCKER_USERNAME` | Docker Hub authentication |
| `DOCKER_PASSWORD` | Docker Hub access token |
| `AZURE_CREDENTIALS` | Service principal for ACA deployment |

## 10. Testing

### 10.1 Test Pyramid

| Layer | Count | Framework | What it tests |
|---|---|---|---|
| Unit tests | 22 | xUnit | Service logic with in-memory repositories |
| Integration tests | 33 | xUnit + WebApplicationFactory | HTTP endpoints through full middleware pipeline |
| E2E browser tests | 12 | Playwright | User journeys via real browser (headless) |
| Smoke tests | 5 | xUnit + HttpClient | Post-deployment verification against live URL |
| **Total** | **72** | | |

### 10.2 Test Infrastructure
- **WebApplicationFactory**: In-process test server with SQLite in-memory database, in-memory repositories, local blob service, and test JWT configuration
- **Playwright**: Headless Chromium with configurable modes (headed, slowmo), health check guard, login/logout helpers
- **Smoke tests**: Skip when `SMOKE_TEST_BASE_URL` environment variable is not set

### 10.3 Test Credentials
| Role | Email | Password |
|---|---|---|
| Admin | admin@cloudsoft.com | Admin123! |
| Candidate | candidate@test.com | Candidate123! |

## 11. Technology Stack

| Component | Technology | Version |
|---|---|---|
| Framework | ASP.NET Core MVC | .NET 10 |
| Language | C# | 13 |
| Identity | ASP.NET Core Identity + SQLite | EF Core 10 |
| Document DB | MongoDB / Azure CosmosDB | MongoDB API 7.0 |
| Blob Storage | Azure.Storage.Blobs / Azurite / Local disk | 12.27.0 |
| Authentication | Cookie + JWT Bearer + Google OAuth + API Key | — |
| CSS Framework | Bootstrap 5 | 5.x |
| Icons | Font Awesome | 6.5.1 (CDN) |
| Testing | xUnit + Playwright | 2.9.3 / 1.x |
| Infrastructure | Azure Bicep | — |
| CI/CD | GitHub Actions | — |
| Containerization | Docker + Docker Compose | — |
| Hosting | Azure Container Apps | — |

## 12. Project Management

Jira is used for work tracking with epics, stories, and tasks. Ticket keys (e.g., `CLO-42`) are referenced in Git branch names and commit messages to create an auditable traceability chain from requirement to deployed code.

## 13. Dependencies Requiring Manual Setup

| Dependency | Action | Who |
|---|---|---|
| Google OAuth | Create OAuth client in Google Cloud Console | Instructor |
| Jira | Create project and epics | Instructor |
| GitHub Secrets | Set via `gh secret set` (Docker Hub and Azure credentials) | Automated (GitHub CLI) |
| Azure deployment | Run `az login` then `infra/deploy.sh` | Automated (Azure CLI) |
| Key Vault secrets | Populated automatically by deploy script | Automated (Azure CLI) |
