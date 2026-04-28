# CloudSoft-Pipeline

Reference project for the ACD course's **CI/CD to Azure Container Apps** exercises under `content/exercises/3-deployment/9-cicd-to-container-apps/`.

## Purpose

A tiny ASP.NET Core MVC application that grows alongside the three CI/CD exercises. The single laboratory surface is the **homepage**, which displays the build commit SHA and hostname so students can see each new revision land on Azure Container Apps.

- **Exercise 1** — First pipeline: GitHub Actions builds the image and pushes to Docker Hub. Azure Container Apps pulls from the public registry. Deployment is manual.
- **Exercise 2** — Private registry: switch to Azure Container Registry, authenticate with a service principal, and have the pipeline run `az containerapp update` itself. Add a smoke test.
- **Exercise 3** — Passwordless OIDC: replace the service principal secret with OIDC federation between GitHub and Microsoft Entra ID. The pipeline still works, but no long-lived password exists.

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

Each exercise corresponds to one or more commits in the live GitHub repository (`larsappel/cloudci`). The state in this directory represents the **final** state after all three exercises are complete: the workflow uses OIDC federation, and no long-lived secrets remain in the repository.

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
