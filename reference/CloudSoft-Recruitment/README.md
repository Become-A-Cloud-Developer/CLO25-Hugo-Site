# CloudSoft Recruitment Portal

Reference implementation for the **Advanced Cloud Development (ACD)** course in the CLO25 program. A .NET 10 MVC application demonstrating progressive evolution from a monolith to containerized microservices on Azure Container Apps.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (for deployment)

## Quick Start

```bash
# Run with Docker Compose (MongoDB + Azurite + App)
docker compose up -d

# App available at http://localhost:5000
```

Or run locally without Docker:

```bash
dotnet run --project src/CloudSoft.Web
```

## Project Structure

```
CloudSoft.slnx                      # Solution file
src/
  CloudSoft.Web/                     # Main MVC application
    Controllers/                     # MVC controllers (Home, Job, Application, Account)
    Data/                            # EF Core DbContext and migrations (Identity only)
    Models/                          # Domain models (Job, Application, ApplicationUser)
    Options/                         # Configuration option classes
    Repositories/                    # Data access (MongoDB + InMemory implementations)
    Services/                        # Business logic layer
    Views/                           # Razor views
tests/
  CloudSoft.Web.Tests/               # Unit tests (xUnit)
infra/
  bicep/                             # Azure Bicep IaC templates
  deploy.sh                          # Full deployment script
  deploy-revision.sh                 # Revision + traffic splitting script
docs/                                # Course documentation
```

## Testing

```bash
dotnet test CloudSoft.slnx
```

## Architecture

- **Web Framework**: ASP.NET Core MVC with Razor views
- **Domain Data**: MongoDB (via MongoDB.Driver)
- **Identity**: ASP.NET Core Identity with SQLite (EF Core)
- **Authentication**: Email/password (Admin), Google OAuth (Candidate)
- **File Storage**: Azure Blob Storage (Azurite locally)
- **Containerization**: Docker with multi-stage build
- **Infrastructure**: Azure Container Apps, CosmosDB (MongoDB API), Key Vault, ACR

## Week-by-Week Progression

| Week | Focus | Key Features |
|------|-------|-------------|
| 1 | Docker + MVC | Job CRUD, Docker Compose, MongoDB |
| 2 | Auth + Cloud | Identity, Google OAuth, ACA deployment |
| 3 | Blob Storage | CV upload, ACA revisions, traffic splitting |

## Configuration

The app uses feature flags to toggle between in-memory and MongoDB repositories:

```json
{
  "FeatureFlags": {
    "UseMongoDB": false
  }
}
```

Set `UseMongoDB: true` and provide a MongoDB connection string for production use.

## Deployment

```bash
# Deploy to Azure
cd infra
./deploy.sh
```

See `docs/` for detailed week-by-week deployment guides.
