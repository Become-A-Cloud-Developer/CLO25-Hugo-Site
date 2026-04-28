# CloudSoft-Pipeline

ACD course reference implementation for two consecutive chapters that share one running .NET MVC app:

- **CI/CD to Azure Container Apps** (`content/exercises/3-deployment/9-cicd-to-container-apps/`) — three exercises walking students from a Docker Hub push, to private-registry auto-deploy on Azure Container Apps, to passwordless OIDC federation between GitHub Actions and Microsoft Entra ID.
- **Logging and Monitoring** (`content/exercises/3-deployment/10-logging-and-monitoring/`) — three follow-on exercises layering observability on top of the same Container App: structured `ILogger<T>` calls, KQL queries against the auto-provisioned Log Analytics workspace, and an Application Insights component for requests, exceptions, and a custom metric.

The single app's homepage displays its build commit SHA and hostname, so each revision deployed by the pipeline is visibly distinct. The Week 5 commits append to the same GitHub repository as Week 4 — there is one continuous history.

## Key files

| File | Purpose |
|------|---------|
| `README.md` | Human-facing overview, layout, run/build commands, exercise mapping |
| `src/CloudCi/` | The MVC application (`dotnet new mvc -o CloudCi`, .NET 10) |
| `src/CloudCi/Dockerfile` | Multi-stage build (.NET SDK → ASP.NET runtime, listens on `:8080`) |
| `src/CloudCi/Views/Home/Index.cshtml` | Modified homepage that reads `BUILD_SHA` env var into a badge |
| `.github/workflows/ci.yml` | Final OIDC-authenticated pipeline (Exercise 3 state) |
| `scripts/validate.mjs` | Node + Playwright smoke check that asserts HTTP 200 and saves a screenshot |
| `src/CloudCi/Controllers/HomeController.cs` | `ILogger<T>` injection + `TelemetryClient` custom metric + `/Boom` action |
| `docs/EXERCISE-VALIDATION-REPORT.md` | Live-execution record (resources, run links, screenshots, deviations) |
| `docs/screenshots/` | Playwright captures of the deployed homepage |
| `docs/validation/` | KQL and App Insights query transcripts (Week 5 evidence) |

## Reference to exercise files

Exercises in the Hugo site that this project supports:

- `content/exercises/3-deployment/9-cicd-to-container-apps/_index.md` — subsection landing (Week 4)
- `content/exercises/3-deployment/9-cicd-to-container-apps/1-first-pipeline-docker-hub.md` — Week 4 Exercise 1
- `content/exercises/3-deployment/9-cicd-to-container-apps/2-private-registry-and-deploy.md` — Week 4 Exercise 2
- `content/exercises/3-deployment/9-cicd-to-container-apps/3-passwordless-deployment-oidc.md` — Week 4 Exercise 3
- `content/exercises/3-deployment/10-logging-and-monitoring/_index.md` — subsection landing (Week 5)
- `content/exercises/3-deployment/10-logging-and-monitoring/1-structured-logging-ilogger.md` — Week 5 Exercise 1
- `content/exercises/3-deployment/10-logging-and-monitoring/2-container-logs-to-log-analytics.md` — Week 5 Exercise 2
- `content/exercises/3-deployment/10-logging-and-monitoring/3-application-insights.md` — Week 5 Exercise 3

## Live resources

| Resource | Value |
|----------|-------|
| GitHub repository | `https://github.com/larsappel/cloudci` |
| Azure subscription | `ca0a7799-8e2e-4237-8616-8cc0e947ecd5` (Lars Appel) |
| Resource group | `rg-cicd-week4` (northeurope) |
| Azure Container Registry | `acrlap8a9f.azurecr.io` |
| Azure Container Apps environment | `cae-cicd-week4` |
| Container App name | `ca-cicd-week4` |
| Log Analytics workspace | `workspace-rgcicdweek4RYcM` (customerId `851d6578-a025-4c8f-8797-d00a06b4662a`) |
| Application Insights component | `cloudci-insights` (appId `914efd2e-6555-4ded-b3df-a59e7ab10c26`, workspace-based) |
| Live URL | `https://ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io/` |
| Failure URL (intentional 500) | `https://ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io/Home/Boom` |
| Entra app (OIDC) | `github-cloudci-oidc` (appId `7c11e4ce-91cd-4ba3-9fce-820669f397fe`) |
| Federated credential subject | `repo:larsappel/cloudci:ref:refs/heads/main` |
