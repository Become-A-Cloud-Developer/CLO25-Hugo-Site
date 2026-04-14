# Docs Reference — CloudSoft Recruitment Portal

This file indexes the documentation for efficient lookup. Read the root [CLAUDE.md](../CLAUDE.md) first for quick-start context.

## Active Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) — Production and local architecture diagrams, data flow, traceability chain
- [FEATURES.md](FEATURES.md) — Feature inventory by role, API endpoints, logging, health checks
- [IAM-ARCHITECTURE.md](IAM-ARCHITECTURE.md) — Authentication and authorization across all boundaries
- [playwright-testing.md](playwright-testing.md) — Browser test setup, modes, and coverage

## Key Files

| Purpose | Path |
|---------|------|
| DI registration & app startup | `src/CloudSoft.Web/Program.cs` |
| Job model (BSON + validation) | `src/CloudSoft.Web/Models/Job.cs` |
| Application model (denormalized) | `src/CloudSoft.Web/Models/Application.cs` |
| Repository interfaces | `src/CloudSoft.Web/Repositories/I*Repository.cs` |
| Service interfaces | `src/CloudSoft.Web/Services/I*Service.cs` |
| Local blob fallback | `src/CloudSoft.Web/Services/LocalBlobService.cs` |
| Auth controller | `src/CloudSoft.Web/Controllers/AccountController.cs` |
| Dockerfile | `src/CloudSoft.Web/Dockerfile` |
| Docker Compose | `docker-compose.yml` |
| Bicep orchestrator | `infra/bicep/main.bicep` |
| Integration test factory | `tests/CloudSoft.Web.Tests/IntegrationTests/CloudSoftWebApplicationFactory.cs` |

## Archive

The `archive/` folder contains early development documents (weeks 1-3 planning and guides) kept for historical reference. **Do NOT read or load archive files during normal development work.** They are obsolete and will pollute the context window with outdated information that conflicts with the current state of the codebase. Only consult the archive if explicitly asked about historical decisions or the original course plan.
