---
title: CloudSoft Recruitment Portal — Project Intent & Course Plan
status: Living document — updated as implementation decisions are made
last_updated: 2026-03-29
scope: Monolith phase (weeks 1–3) implemented; weeks 4–8 planned
---

# CloudSoft Recruitment Portal — Project Intent & Course Plan

## 1. What This Is

Reference implementation for the **Advanced Cloud Development (ACD)** course in the CLO25 program. A .NET 10 MVC recruitment portal that evolves from monolith to containerized microservices on Azure Container Apps over 8 weeks.

Two purposes:
1. **Working reference** — fully tested implementation the instructor can demonstrate and troubleshoot against
2. **Infrastructure as Code** — Bicep templates, Docker Compose, deployment scripts

Students build their own version week by week. This repository stays ahead.

---

## 2. Current State (Monolith — Weeks 1–3)

### Domain Model

```
ApplicationUser (ASP.NET Core Identity + SQLite)
├── Id, Email, PasswordHash
├── DisplayName (Admin)
├── FirstName, LastName (Candidate)
│
├── Role: "Admin" (seeded, email/password login)
│   └── Can: create/edit/delete jobs, view applications, download CVs
│
└── Role: "Candidate" (Google OAuth in production, seeded in dev)
    └── Can: browse jobs, apply with cover letter + CV, view own applications

Admin ──posts──▶ Job ◀──applies── Candidate
                  ├── Title                 Application (denormalized)
                  ├── Description           ├── JobId, JobTitle
                  ├── Location              ├── CandidateId, CandidateEmail, CandidateName
                  ├── Deadline              ├── CoverLetter
                  ├── PostedAt              ├── CvUrl (blob filename)
                  ├── PostedByUserId        └── AppliedAt
                  └── PostedByName
```

### Architecture

```
┌─────────────────────────────────────┐
│    CloudSoft Recruitment Portal     │
│       .NET 10 MVC Monolith          │
│                                     │
│  Razor Views ── Controllers         │
│                    │                │
│              Service Layer          │
│                    │                │
│              Repository Layer       │
│         (MongoDB / InMemory)        │
│                                     │
│  ASP.NET Core Identity (SQLite)     │
│  Google OAuth (cookie auth)         │
│  Blob Storage (Azure / Local)       │
└─────────────────────────────────────┘
```

### Test Accounts (Development)

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@cloudsoft.com` | `Admin123!` (via User Secrets) |
| Candidate | `candidate@test.com` | `Candidate123!` (dev-only seed) |

### Tests

22 total: 13 unit tests (JobService + ApplicationService) + 9 integration tests (user journeys via `WebApplicationFactory`).

```bash
dotnet test CloudSoft.slnx    # No Docker needed
```

---

## 3. Development Environments

Four environments forming a progression. The same application code runs unchanged across all four — only configuration changes.

### Env 1: Fully Local / In-Memory

`dotnet run --project src/CloudSoft.Web` — no external dependencies.

| Component | Implementation |
|-----------|---------------|
| Data | `InMemoryJobRepository` / `InMemoryApplicationRepository` |
| Identity | SQLite file (`identity.db`) |
| Blob storage | `LocalBlobService` → `data/uploads/` directory |
| Google OAuth | Disabled (button hidden) |

### Env 2: Docker Compose

`docker compose up -d` — real services in containers.

| Component | Implementation |
|-----------|---------------|
| Data | MongoDB container |
| Identity | SQLite in Docker volume |
| Blob storage | Azurite container |
| Google OAuth | Optional (via `.env` file) |

### Env 3: Local + Azure Dev Resources

`dotnet run` with User Secrets pointing to Azure services.

| Component | Implementation |
|-----------|---------------|
| Data | CosmosDB (MongoDB API, serverless) |
| Identity | SQLite file (local) |
| Blob storage | Azure Storage Account |
| Google OAuth | Real credentials |

### Env 4: Production (Azure)

Everything in Azure Container Apps with Managed Identity.

| Component | Implementation |
|-----------|---------------|
| Data | CosmosDB (MongoDB API, serverless) |
| Identity | SQLite in Azure Storage volume |
| Blob storage | Azure Storage Account (private) |
| Secrets | Azure Key Vault (via Managed Identity) |
| Hosting | Azure Container Apps + Azure Container Registry |

### Configuration Layering

```
appsettings.json                     ← Base defaults (empty connection strings, UseMongoDB: false)
appsettings.Development.json         ← Local defaults (in-memory repos, no blob, no MongoDB)
Docker Compose environment vars      ← Override for containerized stack
User Secrets                         ← Sensitive values (admin password, Google creds, Azure connection strings)
Azure env vars / Key Vault           ← Production (highest priority)
```

### Environment Progression by Week

| Week | Primary Environment | Transition |
|------|-------------------|-----------|
| 1 | Env 1 → Env 2 | `dotnet run` to Docker Compose |
| 2 | Env 2 → Env 4 | First ACA deployment |
| 3 | Env 3 | Local against Azure dev resources, ACA revisions |
| 4+ | Env 2 + Env 4 | Microservice development locally, deployed to Azure |

---

## 4. Service Implementation Strategy

### Abstraction Interfaces

| Interface | Env 1 (In-Memory) | Env 2 (Docker) | Env 3/4 (Azure) |
|-----------|-------------------|----------------|-----------------|
| `IJobRepository` | `InMemoryJobRepository` | `MongoDbJobRepository` | `MongoDbJobRepository` (CosmosDB) |
| `IApplicationRepository` | `InMemoryApplicationRepository` | `MongoDbApplicationRepository` | `MongoDbApplicationRepository` (CosmosDB) |
| `IBlobService` | `LocalBlobService` | `BlobService` (Azurite) | `BlobService` (Azure Storage) |

Repository selection: `FeatureFlags:UseMongoDB` toggle in config.
Blob service selection: automatic based on whether `BlobStorage:ConnectionString` is set.

### Graceful Degradation

- **Google OAuth**: Conditionally registered when credentials exist. Login view hides the button when unavailable.
- **Blob Storage**: Falls back to `LocalBlobService` (local disk) when no connection string configured.
- **MongoDB**: Falls back to `InMemoryRepository` when `UseMongoDB` is false.

---

## 5. Testing Strategy

### Unit Tests (13)

Service layer tested with in-memory repository implementations. No mocking framework.

- 9 `JobService` tests: CRUD, deadline validation, not-found handling
- 4 `ApplicationService` tests: apply, duplicate prevention, filtering

### Integration Tests (9) — Reference Only

User journey tests via `WebApplicationFactory<Program>`. Not taught to students but present in the reference.

- Fresh factory per test (no shared state)
- Real middleware pipeline: routing, auth, antiforgery, authorization
- Login via POST with seeded credentials + cookie capture
- Antiforgery tokens extracted from HTML responses

Journeys: public browsing, admin CRUD, candidate apply, role enforcement.

---

## 6. Technical Notes

### .NET 10 Specifics

- `public partial class Program { }` no longer needed (ASP0027)
- `AddIdentity<>()` instead of `AddDefaultIdentity<>()` (no Identity UI package)
- `ConfigureExternalAuthenticationProperties()` replaces `ConfigureExternalLoginProperties()`

### Security

- **CSRF**: `AutoValidateAntiforgeryTokenAttribute` as global filter
- **Over-posting**: `[Bind]` on POST actions
- **Cookies**: HttpOnly, SameSite=Lax, SecurePolicy per environment
- **Account lockout**: 5 attempts, 5-minute lockout
- **File uploads**: Extension + content type + magic bytes (`%PDF-`) + server-side filenames + private container
- **Admin password**: User Secrets or env vars only — never in `appsettings.json`
- **Docker**: `.dockerignore`, `127.0.0.1` port binding, non-root USER, Data Protection keys on volume

### Bicep Infrastructure

Modular structure in `infra/bicep/`:

| Module | Resource |
|--------|----------|
| `container-registry.bicep` | ACR Basic with admin user |
| `managed-identity.bicep` | User-assigned identity + Storage Blob Data Contributor |
| `key-vault.bicep` | Key Vault with RBAC + Secrets User role |
| `cosmos-db.bicep` | CosmosDB serverless, MongoDB API 7.0 |
| `log-analytics.bicep` | Log Analytics workspace (required by ACA) |
| `container-apps-env.bicep` | ACA managed environment |
| `container-app.bicep` | Container App, HTTPS ingress, multi-revision |
| `storage-account.bicep` | Storage Account LRS, private containers |

Naming: `cloudsoft-{env}-{resource}` + `uniqueString(resourceGroup().id)`.

---

## 7. Course Context

### Where ACD Fits

| Course | Focus | Compute Model |
|--------|-------|----------------|
| **BCD** (completed) | IaaS, VMs, basic CI/CD | Virtual Machines |
| **ACD** (this course) | Containers, PaaS, microservices | Azure Container Apps |
| Course 3 | Serverless, cloud-native | Azure Functions |
| Course 4 | Kubernetes, orchestration | AKS |

### What Students Know from BCD

- .NET MVC, repository pattern, DI, feature flags
- MongoDB/CosmosDB, Azure Blob Storage, Key Vault
- Configuration layering, GitHub Actions, Bicep/ARM
- Docker (for infrastructure), Linux, networking

### The BCD Reference: CloudSoft Newsletter

Single-entity newsletter app. Key patterns ACD extends:
- `ISubscriberRepository` with swappable implementations → `IBlobService`, `IMessagePublisher`
- Feature flag toggle → environment-based provider selection
- Docker Compose for MongoDB → Docker Compose for full application stack
- SCP + systemd → container image push + ACA deployment

### Architecture Style

**Deliberate decision**: Practical three-layer, not DDD/Clean Architecture. This is a cloud course, not a software architecture course. Each service is one project, not six.

---

## 8. Architectural Progression (Weeks 4–8)

### Week 4: Microservice Split (Backend-for-Frontend)

```
                ┌─────────────────────────┐
                │    CloudSoft Web App     │
                │    (Razor MVC Frontend)  │
                │    Identity + Cookies    │
                └──────┬────────┬──────────┘
                       │        │
          ┌────────────┘        └────────────┐
          ▼                                   ▼
┌─────────────────┐               ┌──────────────────┐
│   Job Service   │               │ Application Svc  │
│   (.NET API)    │               │   (.NET API)     │
│  ┌───────────┐  │               │  ┌────────────┐  │
│  │ CosmosDB  │  │               │  │ CosmosDB   │  │
│  └───────────┘  │               │  └────────────┘  │
└─────────────────┘               └──────────────────┘
```

Frontend keeps cookies. Backend APIs receive identity via `X-User-Id`/`X-User-Role` headers (internal ingress only).

### Week 5: Async Messaging

Application Service publishes to Service Bus (RabbitMQ locally) → Notification Worker sends email. Queue-based ACA scaling.

### Weeks 6–7: Production Hardening

CI/CD pipeline, container scanning, Bicep for all resources, Application Insights, health probes, VNet integration, cookie hardening.

### Week 8: Summary

Full architecture review (VM → containers → microservices), cost comparison, retrospective.

---

## 9. Week-by-Week Detail

### Week 1 — Docker + Job CRUD (No Auth)

**Cloud**: Containers — from VMs to Docker
**App**: Job CRUD (Razor views, repository pattern)
**Skills**: Dockerfile, Docker Compose (app + MongoDB + Azurite)
**Exercise**: Containerize the monolith, run with Docker Compose

### Week 2 — Auth + ACA Deployment

**Cloud**: Running containers in the cloud
**App**: Identity, Google OAuth, candidate applications, CV upload
**Skills**: ACR, ACA deployment, Managed Identity, Key Vault
**Auth**: Docker volumes for SQLite, Key Vault for Google secrets

### Week 3 — Revisions + Blob Storage

**Cloud**: Production deployment patterns
**App**: CV upload refinement
**Skills**: ACA revisions, traffic splitting, scaling, Data Protection keys
**Auth**: Shared Data Protection keys across ACA revisions

### Week 4 — Microservice Architecture

**Cloud**: Multi-container environments
**App**: Extract Job API + Application API
**Skills**: Internal/external ingress, service-to-service communication, Swagger
**Auth**: Trusted internal headers (`X-User-Id`, `X-User-Role`)

### Week 5 — Async Communication

**Cloud**: Message queues, event-driven architecture
**App**: Notification Worker (BackgroundService)
**Skills**: Service Bus, queue-based scaling, dead-letter queues
**Interfaces**: `IMessagePublisher` (RabbitMQ / ServiceBus), `IEmailSender` (Console / SendGrid)

### Week 6 — CI/CD + Infrastructure as Code

**Cloud**: Production-grade pipelines
**App**: No new features — harden and automate
**Skills**: GitHub Actions, container scanning, Bicep, environment promotion

### Week 7 — Observability + Security

**Cloud**: Operating microservices in production
**App**: Cookie hardening, rate limiting, health checks
**Skills**: Application Insights, distributed tracing, VNet, private endpoints

---

## 10. Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Compute | Azure Container Apps | Deep single-platform learning. ACI too simple, App Service wrong mental model. |
| Frontend | Razor MVC | Known from BCD. SPA saved for Course 3. |
| APIs | MVC `ControllerBase` | Minimal diff from frontend: `return View()` → `return Ok()` |
| Auth | Identity + Google OAuth | Two paths match domain (admin vs candidate). |
| Identity store | SQLite | Identity is relational. Teachable separation from document store. |
| Roles | Admin + Candidate | Renamed from "Company". Single-company portal, admin seeded not registered. |
| Auth timing | Week 2 (not Week 1) | Week 1 focuses on Docker + CRUD without auth complexity. |
| Local messaging | RabbitMQ | Management UI, forces abstraction, lightweight. |
| Architecture | Three-layer | Cloud course, not architecture course. |
| Monolith split | Backend-for-Frontend | Frontend keeps cookies. No frontend rewrite. |

---

## 11. What's NOT Covered (Later Courses)

- **Kubernetes** (Course 4) — ACA abstracts it
- **Serverless / Azure Functions** (Course 3)
- **SPA / React / Angular** (Course 3)
- **DDD / Clean Architecture** — Practical three-layer is sufficient
- **JWT / OAuth2 token flows** — Cookie auth covers the use cases
- **API Gateway** (Azure API Management) — Frontend acts as gateway

---

## 12. Azure Service Mapping

| Azure Service | Purpose | Local Equivalent |
|--------------|---------|-----------------|
| CosmosDB (MongoDB API) | Domain data | MongoDB container / InMemory |
| Blob Storage | CV PDFs | Azurite / LocalBlobService |
| Key Vault | Secrets | User Secrets |
| Service Bus (week 5) | Async messaging | RabbitMQ |
| Application Insights (week 7) | Telemetry | Console logging |
| Container Registry | Images | Local Docker build |
| Container Apps | Hosting | Docker Compose / dotnet run |

---

## 13. Assignment Structure

| Assignment | Weeks | Focus |
|------------|-------|-------|
| 1 | 1–3 | Containerize and deploy (Docker, ACR, ACA) |
| 2 | 4–5 | Microservices with async communication |
| 3 | 6–7 | CI/CD, observability, security |
