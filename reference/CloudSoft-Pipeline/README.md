# CloudSoft-Pipeline

Reference project for two consecutive ACD chapters that share one running ASP.NET Core MVC application:

- **CI/CD to Azure Container Apps** — `content/exercises/3-deployment/9-cicd-to-container-apps/`
- **Logging and Monitoring** — `content/exercises/3-deployment/10-logging-and-monitoring/`

## Purpose

A tiny ASP.NET Core MVC application that grows across two chapters of exercises. The single laboratory surface is the **homepage**, which displays the build commit SHA and hostname so students can see each new revision land on Azure Container Apps. By the end of the second chapter the same homepage is also instrumented with `ILogger<T>` and Application Insights.

- **Week 4 Exercise 1** — First pipeline: GitHub Actions builds the image and pushes to Docker Hub. Azure Container Apps pulls from the public registry. Deployment is manual.
- **Week 4 Exercise 2** — Private registry: switch to Azure Container Registry, authenticate with a service principal, and have the pipeline run `az containerapp update` itself. Add a smoke test.
- **Week 4 Exercise 3** — Passwordless OIDC: replace the service principal secret with OIDC federation between GitHub and Microsoft Entra ID. The pipeline still works, but no long-lived password exists.
- **Week 5 Exercises 1–3** — Observability: structured `ILogger<T>` with semantic message templates, container-log queries against the auto-provisioned Log Analytics workspace using KQL, then Application Insights with Live Metrics, an Application Map, a Failures blade fed by a `/Home/Boom` action, and a `home-page-views` custom metric. The chapter ends by tearing down the resource group and the Entra app registration.

## Layout

```text
reference/CloudSoft-Pipeline/
├── src/CloudCi/                        # The MVC application (dotnet new mvc -o CloudCi)
│   ├── CloudCi.csproj
│   ├── Dockerfile                      # Multi-stage build (.NET SDK → ASP.NET runtime)
│   ├── .dockerignore
│   ├── Views/Home/Index.cshtml         # Homepage with build SHA + host badges
│   └── ...
├── .github/workflows/ci.yml            # Final OIDC-authenticated pipeline (after Ex 3)
├── scripts/validate.mjs                # Playwright validation script
├── docs/
│   ├── EXERCISE-VALIDATION-REPORT.md   # Live-execution validation record
│   └── screenshots/                    # Playwright captures
└── README.md
```

## Running locally

```bash
cd src/CloudCi
dotnet run
```

The app prints the port at startup (typically `http://localhost:5XXX`). Visit the homepage to see the build SHA badge — it shows `local` when run outside the pipeline.

## Building the container locally

```bash
cd src/CloudCi
docker build --build-arg BUILD_SHA=dev-local -t cloudci:local .
docker run --rm -p 8080:8080 cloudci:local
```

Then open `http://localhost:8080`. The badge should show `build: dev-local`.

## Exercise progression

Each exercise corresponds to one or more commits in the live GitHub repository (`larsappel/cloudci`). Week 5 commits append to the same history as Week 4 — there is one continuous timeline. The state in this directory represents the **final** state after all six exercises (across both chapters) are complete: OIDC-federated workflow, structured `ILogger` calls, request correlation IDs, App Insights SDK wired up, `/Home/Boom` for inducing exceptions, and a `home-page-views` custom metric.

## Live deployment

See `docs/EXERCISE-VALIDATION-REPORT.md` for the live URL, resource names, GitHub Actions run links, and manual verification steps.

## Validation

Smoke-check the live deployment with the included Playwright script:

```bash
cd scripts
npm install
npx playwright install chromium
FQDN=<container-app-fqdn> LABEL=manual-check node validate.mjs
```

The script asserts HTTP 200, reads the build SHA badge, and saves a screenshot to `docs/screenshots/<label>.png`.
