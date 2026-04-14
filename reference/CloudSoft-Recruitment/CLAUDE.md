# CloudSoft Recruitment Portal

ACD course reference implementation. .NET 10 MVC monolith (weeks 1–3), evolving to microservices (weeks 4+).

## Quick Commands

```bash
dotnet run --project src/CloudSoft.Web     # Run locally (in-memory, no Docker)
docker compose up -d                        # Run with MongoDB + Azurite
dotnet test CloudSoft.slnx                  # 22 tests, no Docker needed
dotnet build CloudSoft.slnx                 # Build solution
az bicep build --file infra/bicep/main.bicep  # Validate Bicep
```

## Test Accounts (Development)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@cloudsoft.com | Admin123! |
| Candidate | candidate@test.com | Candidate123! |

Set via: `dotnet user-secrets set "AdminSeed:Password" "Admin123!" --project src/CloudSoft.Web`

## Project Layout

```
src/CloudSoft.Web/          MVC app (controllers, views, services, repositories)
tests/CloudSoft.Web.Tests/  Unit tests + integration tests
infra/bicep/                Azure Bicep IaC modules
infra/deploy.sh             Full Azure deployment
docker-compose.yml          Local dev stack (MongoDB, Azurite, app)
docs/                       Course documentation and intent
```

## Key Patterns

- **Repository toggle**: `FeatureFlags:UseMongoDB` switches InMemory ↔ MongoDB
- **Blob fallback**: No connection string → `LocalBlobService` (disk); with connection string → `BlobService` (Azurite/Azure)
- **Google OAuth**: Conditionally registered; login button hidden when not configured
- **Roles**: `[Authorize(Roles = "Admin")]` for job management, `[Authorize(Roles = "Candidate")]` for applications

## Environments

1. **In-memory** (`dotnet run`) — no dependencies, fastest iteration
2. **Docker Compose** (`docker compose up`) — MongoDB + Azurite in containers
3. **Local + Azure** (`dotnet run` + User Secrets with Azure connection strings)
4. **Production** (ACA + CosmosDB + Blob Storage + Key Vault)

## Documentation

- [docs/CLAUDE.md](docs/CLAUDE.md) — Documentation index and key file reference
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — Production and local architecture diagrams
- [docs/FEATURES.md](docs/FEATURES.md) — Feature inventory by role, API, logging, health checks
- [docs/IAM-ARCHITECTURE.md](docs/IAM-ARCHITECTURE.md) — Authentication and authorization boundaries
- [docs/archive/](docs/archive/CLAUDE.md) — Early development documents (weeks 1-3), kept for reference
