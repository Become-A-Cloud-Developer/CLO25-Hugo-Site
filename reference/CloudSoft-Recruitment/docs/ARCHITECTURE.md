# CloudSoft Recruitment Portal — Architecture Overview

## Production Architecture (Azure)

```mermaid
graph TB
    subgraph Users
        browser["Browser<br/><i>Admin & Candidate</i>"]
        apiclient["API Consumer<br/><i>Partner job boards, mobile apps</i>"]
    end

    subgraph External
        google["Google OAuth<br/><i>Candidate authentication</i>"]
        restcountries["REST Countries API<br/><i>Location validation</i>"]
        jira["Jira<br/><i>Work items & traceability</i>"]
    end

    subgraph CI/CD
        github["GitHub<br/><i>Source code & PRs</i>"]
        actions["GitHub Actions<br/><i>Build, test, push image</i>"]
    end

    subgraph Azure["Azure Production Environment"]
        acr["Azure Container Registry<br/><i>Docker images tagged<br/>with git commit hash</i>"]

        subgraph ACA["Azure Container Apps"]
            app["CloudSoft.Web<br/><i>.NET 10 MVC + API</i><br/><br/>GET /health — liveness probe<br/>GET /version — commit hash<br/>GET /api/jobs — public API"]
        end

        cosmosdb["Azure CosmosDB<br/><i>MongoDB API</i><br/><br/>Jobs collection<br/>Applications collection"]

        blob["Azure Blob Storage<br/><i>Private container</i><br/><br/>CV PDF files<br/>GUID-based filenames"]

        keyvault["Azure Key Vault<br/><i>Secrets management</i><br/><br/>Connection strings<br/>Feature flags<br/>Google OAuth credentials"]

        appinsights["Application Insights<br/><i>Azure Monitor</i><br/><br/>Request telemetry<br/>Structured logs<br/>KQL queries"]
    end

    %% User flows
    browser -- "HTTPS" --> app
    apiclient -- "REST API<br/>/api/jobs" --> app

    %% App to Azure services
    app -- "Read/write jobs<br/>& applications" --> cosmosdb
    app -- "Upload/download<br/>CV files" --> blob
    app -- "Read secrets<br/>at startup" --> keyvault
    app -- "Telemetry &<br/>structured logs" --> appinsights

    %% App to external
    app -- "OAuth callback" --> google
    app -- "Location lookup" --> restcountries

    %% CI/CD flow
    jira -. "Ticket keys in<br/>branches & commits" .-> github
    github -- "Push / PR merge" --> actions
    actions -- "docker push<br/>RBAC auth" --> acr
    acr -- "Pull image<br/>on deployment" --> app

    %% Styling
    classDef azure fill:#0078d4,color:#fff,stroke:#005a9e
    classDef external fill:#4a4a4a,color:#fff,stroke:#333
    classDef cicd fill:#24292e,color:#fff,stroke:#1a1a1a
    classDef user fill:#107c10,color:#fff,stroke:#0b5e0b

    class app,cosmosdb,blob,keyvault,appinsights,acr azure
    class google,restcountries,jira external
    class github,actions cicd
    class browser,apiclient user
```

## Runtime Data Flow

| Flow | From | To | What |
|------|------|----|------|
| Browse/apply | Browser | Container App | HTTPS, cookie auth |
| API access | API consumer | Container App | REST, JSON responses |
| Identity / auth | Container App | SQLite (ephemeral) | Local filesystem, recreated on startup |
| Job/application data | Container App | CosmosDB | MongoDB wire protocol |
| CV storage | Container App | Blob Storage | PDF upload/download |
| Secrets | Key Vault | Container App | Connection strings, credentials |
| Auth | Container App | Google | OAuth 2.0 redirect flow |
| Location lookup | Container App | REST Countries | HTTP GET, JSON response |
| Telemetry | Container App | Application Insights | Structured logs, request traces |
| Health probes | Container Apps platform | Container App | GET /health, liveness/readiness |
| Image pull | Container Registry | Container App | Docker image tagged with commit hash |

## Local Development Architecture

```mermaid
graph TB
    subgraph Users
        browser["Browser<br/><i>localhost:5000</i>"]
    end

    subgraph External
        google["Google OAuth<br/><i>Optional — hidden<br/>when not configured</i>"]
        restcountries["REST Countries API<br/><i>Location validation</i>"]
    end

    subgraph Laptop["Developer Laptop"]
        subgraph Docker["Docker Compose"]
            app["CloudSoft.Web<br/><i>.NET 10 MVC + API</i><br/><br/>Port 5000 → 8080<br/>ASPNETCORE_ENVIRONMENT=Development"]

            mongodb["MongoDB<br/><i>mongo:latest</i><br/><br/>Jobs collection<br/>Applications collection<br/>Port 27017"]

            azurite["Azurite<br/><i>Azure Storage emulator</i><br/><br/>CV PDF files<br/>Ports 10000–10002"]
        end

        identity["SQLite<br/><i>identity.db</i><br/><br/>ASP.NET Core Identity<br/>Users & roles"]

        secrets["User Secrets<br/><i>dotnet user-secrets</i><br/><br/>Admin password<br/>Google OAuth credentials"]

        console["Console output<br/><i>Structured logs<br/>in readable format</i>"]
    end

    %% User flows
    browser -- "HTTP" --> app

    %% App to local services
    app -- "Read/write jobs<br/>& applications" --> mongodb
    app -- "Upload/download<br/>CV files" --> azurite
    app -- "Read/write<br/>users & roles" --> identity
    app -- "Read secrets<br/>at startup" --> secrets
    app -- "ILogger output" --> console

    %% App to external
    app -. "OAuth callback<br/>(if configured)" .-> google
    app -- "Location lookup" --> restcountries

    %% Styling
    classDef docker fill:#2496ed,color:#fff,stroke:#1a7dc4
    classDef local fill:#6c757d,color:#fff,stroke:#555
    classDef external fill:#4a4a4a,color:#fff,stroke:#333
    classDef user fill:#107c10,color:#fff,stroke:#0b5e0b

    class app,mongodb,azurite docker
    class identity,secrets,console local
    class google,restcountries external
    class browser user
```

### Local vs Production Service Mapping

| Local (Docker Compose) | Production (Azure) | Purpose |
|---|---|---|
| MongoDB container | Azure CosmosDB (MongoDB API) | Job & application data |
| Azurite container | Azure Blob Storage | CV file storage |
| SQLite file | SQLite file (ephemeral, recreated on startup) | Identity / auth |
| User Secrets | Azure Key Vault | Secrets management |
| Console output | Application Insights | Logging & telemetry |
| Docker Compose | Azure Container Apps | Container orchestration |
| Local Docker build | Azure Container Registry | Image storage |

## Runtime Scenarios

The same Docker image supports four runtime scenarios via configuration and graceful degradation:

| Scenario | Data | Blobs | Identity | Secrets | Telemetry |
|---|---|---|---|---|---|
| **1. In-Memory Local** | In-memory repos | Local disk | SQLite | User Secrets | Console |
| **2. Docker Compose** | MongoDB container | Azurite container | SQLite (Docker volume) | Environment vars | Console |
| **3. Azure In-Memory** | In-memory repos | Local disk | SQLite (ephemeral) | Key Vault | Application Insights |
| **4. Full Production** | CosmosDB | Azure Blob Storage | SQLite (ephemeral) | Key Vault | Application Insights |

Scenario 3 is an intermediate step for verifying the deployment pipeline works (container image, ACA, health probes, Key Vault) without needing CosmosDB or Blob Storage provisioned.

## Traceability Chain

```
Jira ticket (CLO-42)
  → Git branch (feature/CLO-42-structured-logging)
    → Commits (CLO-42: add structured logging)
      → Pull request (GitHub)
        → GitHub Actions build
          → Docker image (cloudsoft-web:a1b2c3d)
            → Azure Container Registry
              → Container App running image
                → GET /version → "1.0.0+a1b2c3d"
```
