# CloudSoft-Api

ACD course reference implementation for the **REST API and DTOs** chapter (Week 6) under `content/exercises/4-services-and-apis/1-rest-api-and-dtos/`. This project supports a three-exercise series in which students build a small ASP.NET Core Web API (`CloudCiApi`) exposing a quotes endpoint, then progressively gate it — first with an `ApiKeyMiddleware` reading a `X-Api-Key` header backed by a Container Apps secret, then with JWT bearer authentication issued by a `TokensController` from an in-memory user list. The laboratory surface is Swagger UI plus the wire response from `/api/quotes`; both change observably as each exercise lands. The final exercise also tears down the resource group and the Entra OIDC app registration.

## Key files

| File | Purpose |
|------|---------|
| `README.md` | Human-facing overview, layout, run/build commands, exercise mapping |
| `src/CloudCiApi/` | The ASP.NET Core Web API (mirrored from live execution in Phase 5) |
| `.github/workflows/ci.yml` | Final OIDC-authenticated pipeline state |
| `scripts/validate.mjs` | Node validation harness (added in Phase 4) |
| `docs/EXERCISE-VALIDATION-REPORT.md` | Live-execution record (resources, run links, screenshots, deviations) |
| `docs/screenshots/` | Swagger UI and curl-output captures |
| `docs/validation/` | curl transcripts and JWT decode evidence |

## Reference to exercise files

Exercises in the Hugo site that this project supports:

- `content/exercises/4-services-and-apis/1-rest-api-and-dtos/_index.md` — subsection landing
- `content/exercises/4-services-and-apis/1-rest-api-and-dtos/1-rest-controllers-and-dtos.md` — Exercise 1
- `content/exercises/4-services-and-apis/1-rest-api-and-dtos/2-api-key-middleware.md` — Exercise 2
- `content/exercises/4-services-and-apis/1-rest-api-and-dtos/3-jwt-bearer-and-cleanup.md` — Exercise 3

## Live resources

| Resource | Value |
|----------|-------|
| GitHub repository | <https://github.com/larsappel/cloudci-api> |
| Azure subscription | `ca0a7799-8e2e-4237-8616-8cc0e947ecd5` (Lars Appel) |
| Resource group | `rg-api-week6` (northeurope) — deleted by the Ex 6.3 cleanup substep |
| Azure Container Registry | `acrapi8fc7b7.azurecr.io` |
| Azure Container Apps environment | `cae-api-week6` |
| Container App name | `ca-api-week6` |
| Application Insights component | `cloudci-api-insights` (appId `6ee83f78-a98d-4482-81cd-9af58339584e`, workspace-based against `workspace-rgapiweek66KfP`) |
| Container App secret (API key) | `api-key` (env var `ApiKey__Value=secretref:api-key`) |
| Container App secret (JWT signing key) | `jwt-signing-key` (env var `Jwt__SigningKey=secretref:jwt-signing-key`; plus `Jwt__Issuer=cloudci-api-prod` and `Jwt__Audience=cloudci-api-clients-prod` as plain env vars) |
| Live URL | `https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/` |
| Swagger URL | `https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/swagger` |
| Entra app (OIDC) | `github-cloudci-api-oidc` (appId `cbe3d715-650e-44d6-9802-381870be8c63`) — deleted by the Ex 6.3 cleanup substep |
| Federated credential subject | `repo:larsappel/cloudci-api:ref:refs/heads/main` |

(Captured during the 2026-04-28 live-execution run. The resource group and Entra app are torn down at the end of the chapter; the GitHub secrets remain in `larsappel/cloudci-api` as stale-but-inert artefacts unless explicitly deleted.)
