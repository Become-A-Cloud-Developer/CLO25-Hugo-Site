# CloudSoft-Careers

ACD course reference implementation for the **File Uploads and Deep Health Probes** chapter (Week 7) under `content/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/`. This project supports a three-exercise series in which students build a small ASP.NET Core MVC app (`CloudCiCareers.Web`) — an anonymous recruitment portal whose home page lists hard-coded job postings, whose apply form accepts a PDF CV (validated via magic-bytes), and whose recruiter pages browse, edit, download, and delete submitted applications. The persistence layer is transformed across the three exercises: deliberately fragile in-memory + local-file in Ex 1, durable Cosmos DB + Azure Blob Storage (both managed-identity authenticated) in Ex 2, and observable through deep health probes (`/health/live`, `/health/ready`, `/health`) wired to Container Apps liveness + readiness in Ex 3. The laboratory surface is the home page plus the recruiter listing; both stay visually identical across exercises while the storage substrate changes underneath. The final exercise also tears down the resource group and the Entra OIDC app registration.

## Key files

| File | Purpose |
|------|---------|
| `README.md` | Human-facing overview, layout, run/build commands, exercise mapping |
| `src/CloudCiCareers.Web/` | The ASP.NET Core MVC app (mirrored from live execution in Phase 5) |
| `.github/workflows/ci.yml` | Final OIDC-authenticated pipeline state |
| `scripts/validate.mjs` | Node validation harness (added in Phase 4) |
| `docs/EXERCISE-VALIDATION-REPORT.md` | Live-execution record (resources, run links, screenshots, deviations) |
| `docs/screenshots/` | Home page, apply form, thanks, recruiter listing, detail, validation error |
| `docs/validation/` | Health-probe curl matrix, App Insights KQL transcripts, probe-config JSON, persistence-survives-rollover transcript |

## Reference to exercise files

Exercises in the Hugo site that this project supports:

- `content/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/_index.md` — subsection landing
- `content/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/1-mvc-uploads-and-pdf-validation.md` — Exercise 1
- `content/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/2-cosmos-and-blob-via-managed-identity.md` — Exercise 2
- `content/exercises/6-storage-and-resilience/1-uploads-and-deep-probes/3-deep-health-probes-and-cleanup.md` — Exercise 3

## Live resources

| Resource | Value |
|----------|-------|
| GitHub repository | <https://github.com/larsappel/cloudci-careers> |
| Azure subscription | `ca0a7799-8e2e-4237-8616-8cc0e947ecd5` (Lars Appel) |
| Resource group | `rg-careers-week7` (northeurope) — deleted by the Ex 7.3 cleanup substep |
| Azure Container Registry | `acrcareers797b40.azurecr.io` |
| Azure Container Apps environment | `cae-careers-week7` |
| Container App name | `ca-careers-week7` (principalId `533bcfbe-9974-4009-b3f0-48c2cfb5e102`) |
| Container App FQDN | `ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io` |
| Application Insights component | `cloudci-careers-insights` (appId `92560597-5383-41ca-9485-c8226a06190b`, workspace `workspace-rgcareersweek7dvVX` customerId `8acbc420-fe03-4326-bc3f-149d9970f5ea`) |
| Application Insights wiring | Connection string injected via Container Apps secret `appinsights-connstr`, env var `APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connstr` |
| Cosmos DB account | `cosmos-careers-797b40` (serverless), database `careers`, container `applications` (partition key `/id`) |
| Cosmos DB endpoint | `https://cosmos-careers-797b40.documents.azure.com:443/` (env var `Cosmos__Endpoint`, plain — managed-identity-authenticated, no key) |
| Cosmos DB data-plane role assignment | Built-in `Cosmos DB Built-in Data Contributor` role definition `00000000-0000-0000-0000-000000000002` granted to the Container App's system-assigned managed identity (data-plane RBAC, **separate from** Azure RBAC) |
| Storage account | `stcareers797b40`, container `cvs` (private access) |
| Storage Blob endpoint | `https://stcareers797b40.blob.core.windows.net` (env var `Storage__BlobEndpoint`, plain) |
| Storage Blob role assignment | `Storage Blob Data Contributor` granted to the Container App's system-assigned managed identity |
| Container App probes | Liveness on `/health/live:8080`; Readiness on `/health/ready:8080`. Wired via `az containerapp update --yaml` (deviation #5 in the validation report). |
| Live URL | `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/` |
| Entra app (OIDC) | `github-cloudci-careers-oidc` (appId `019c8643-903c-40f2-9aa5-dae4ff0bd737`, tenant `6ee71fa2-3288-478d-a39f-fa453d0984f5`) — deleted by the Ex 7.3 cleanup substep |
| Federated credential subject | `repo:larsappel/cloudci-careers:ref:refs/heads/main` |

(Captured during the 2026-04-28 live-execution run. The resource group and Entra app are deferred for cleanup; the GitHub secrets remain in `larsappel/cloudci-careers` as stale-but-inert artefacts unless explicitly deleted.)
