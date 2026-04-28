# CloudSoft-Pipeline

ACD course reference implementation for the **CI/CD to Azure Container Apps** exercise series. A minimal ASP.NET Core MVC app whose homepage displays its build commit SHA and hostname, so each revision deployed by the pipeline is visibly distinct. This project is the reference for `content/exercises/3-deployment/9-cicd-to-container-apps/` — three exercises that walk students from a Docker Hub push, to private-registry auto-deploy on Azure Container Apps, to passwordless OIDC federation between GitHub Actions and Microsoft Entra ID.

## Key files

| File | Purpose |
|------|---------|
| `README.md` | Human-facing overview, layout, run/build commands, exercise mapping |
| `src/CloudCi/` | The MVC application (`dotnet new mvc -o CloudCi`, .NET 10) |
| `src/CloudCi/Dockerfile` | Multi-stage build (.NET SDK → ASP.NET runtime, listens on `:8080`) |
| `src/CloudCi/Views/Home/Index.cshtml` | Modified homepage that reads `BUILD_SHA` env var into a badge |
| `.github/workflows/ci.yml` | Final OIDC-authenticated pipeline (Exercise 3 state) |
| `scripts/validate.mjs` | Node + Playwright smoke check that asserts HTTP 200 and saves a screenshot |
| `docs/EXERCISE-VALIDATION-REPORT.md` | Live-execution record (resources, run links, screenshots, deviations) |
| `docs/screenshots/` | Playwright captures of the deployed homepage |

## Reference to exercise files

Exercises in the Hugo site that this project supports:

- `content/exercises/3-deployment/9-cicd-to-container-apps/_index.md` — subsection landing
- `content/exercises/3-deployment/9-cicd-to-container-apps/1-first-pipeline-docker-hub.md` — Exercise 1
- `content/exercises/3-deployment/9-cicd-to-container-apps/2-private-registry-and-deploy.md` — Exercise 2
- `content/exercises/3-deployment/9-cicd-to-container-apps/3-passwordless-deployment-oidc.md` — Exercise 3

## Live resources

| Resource | Value |
|----------|-------|
| GitHub repository | `https://github.com/larsappel/cloudci` |
| Azure subscription | `ca0a7799-8e2e-4237-8616-8cc0e947ecd5` (Lars Appel) |
| Resource group | `rg-cicd-week4` (northeurope) |
| Azure Container Registry | `acrlap8a9f.azurecr.io` |
| Azure Container Apps environment | `cae-cicd-week4` |
| Container App name | `ca-cicd-week4` |
| Live URL | `https://ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io/` |
| Entra app (OIDC) | `github-cloudci-oidc` (appId `7c11e4ce-91cd-4ba3-9fce-820669f397fe`) |
| Federated credential subject | `repo:larsappel/cloudci:ref:refs/heads/main` |
