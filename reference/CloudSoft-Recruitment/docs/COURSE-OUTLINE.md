# ACD Course Outline — CloudSoft Recruitment Portal

8-week course. Weeks 1–7 introduce new content; week 8 is repetition and summary.

## Week 1 — Agile Workflow & Inner Loop

- Jira: boards, sprints, backlog, user stories
- Git branching, commits, pull requests, code review
- Inner-loop development: edit → build → run → test
- `dotnet run` with in-memory repositories (no external dependencies)

## Week 2 — Docker & Docker Compose

- Containers vs VMs, images, layers, registries
- Multi-stage Dockerfile (`sdk` → `aspnet` runtime)
- Building and pushing images to Docker Hub
- Multi-platform builds (`linux/amd64`, `linux/arm64`)
- Docker Compose: app + MongoDB + Mongo Express + Azurite
- Feature-flag toggle: `UseMongoDB` switches InMemory → MongoDB

## Week 3 — Authentication & Authorization (Part 1)

- ASP.NET Core Identity with cookie authentication
- User model (`ApplicationUser`), roles (Admin, Candidate)
- Registration, login, logout, account lockout
- `[Authorize]` and `[Authorize(Roles = "...")]`
- CSRF protection (`AutoValidateAntiforgeryToken`)
- Local-only (no Docker), no social login yet

## Week 4 — CI/CD & Azure Deployment

- GitHub Actions workflow: build → test → Docker build → push → deploy
- Azure Container Registry (ACR)
- Azure Container Apps (ACA) deployment
- Service principal credentials first, OIDC federation as stretch goal
- Version tagging with git SHA (`1.0.0+abc1234`)
- Post-deploy verification (health, version endpoints)

## Week 5 — Logging & Monitoring

- Structured logging with `ILogger<T>` and log levels
- Application Insights: telemetry, connection string, feature flag
- Log Analytics workspace
- Azure Monitor dashboards and live metrics
- Correlating logs across requests

## Week 6 — REST API & DTOs

- API controllers alongside MVC (`/api/jobs`, `/api/token`)
- DTOs: `JobResponse`, `ApplicationResponse`, `CreateJobRequest`, `TokenRequest/Response`
- JWT bearer authentication (`JwtTokenService`, `TokenController`)
- API key middleware (`X-API-Key` header)
- Swagger / OpenAPI documentation
- Client-side consumption pattern (SPA → API)

## Week 7 — Blob Storage & Health Endpoints

- File upload: PDF validation (extension, content-type, magic bytes, 5 MB limit)
- `IBlobService` with two implementations: `BlobService` (Azure/Azurite) ↔ `LocalBlobService` (disk)
- Azure Storage Account, blob containers, managed identity access
- Health check endpoints (`/health`) with component checks
- Version endpoint (`/version`)
- Google OAuth as social login (stretch: HTTPS in Docker)

## Week 8 — Repetition & Summary

- Review of full architecture (local → Docker → CI/CD → Azure)
- End-to-end walkthrough of a feature across all layers
- Q&A and exam preparation
