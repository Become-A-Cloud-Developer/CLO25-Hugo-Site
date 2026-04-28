# Exercise Validation Report — CloudSoft-Api

## Overview

Live-execution validation run on **2026-04-28** that walked all three exercises of the **REST API and DTOs** chapter end-to-end against a fresh Azure resource group and a fresh GitHub repository. The pipeline deployed five times — once per exercise step that ships code (Ex 1 scaffold + pipeline, Ex 1 App Insights SDK, Ex 2 API-key middleware, follow-up smoke-test scope fix, Ex 3 JWT replacement). Every workflow run finished green and every Test-Your-Implementation assertion passed against the deployed Container App. The chapter-final cleanup substep (`az group delete` + `az ad app delete`) executed at the end of the run; the resource group was queued for asynchronous deletion and the Entra app registration was deleted from the tenant.

## Resources Provisioned

| Resource | Name | Region | Notes |
|----------|------|--------|-------|
| Resource group | `rg-api-week6` | `northeurope` | Holds every Azure resource for the chapter; deleted during the Ex 6.3 cleanup substep. |
| Azure Container Registry | `acrapi8fc7b7` | `northeurope` | Basic SKU, admin user disabled, AcrPush granted to the OIDC app, AcrPull granted to the Container App's managed identity. |
| Container Apps environment | `cae-api-week6` | `northeurope` | Auto-managed Log Analytics workspace `workspace-rgapiweek66KfP` provisioned alongside. |
| Container App | `ca-api-week6` | `northeurope` | Single replica, ingress port 8080, system-assigned managed identity used to pull images from ACR. |
| Application Insights component | `cloudci-api-insights` | `northeurope` | Workspace-based against `workspace-rgapiweek66KfP`. AppId `6ee83f78-a98d-4482-81cd-9af58339584e`. Connection string injected via Container Apps secret. |
| Container App secret (API key) | `api-key` | — | 64-byte base64 value generated locally with `openssl rand -base64 48`; referenced via `secretref:` from env var `ApiKey__Value`. Removed implicitly when the resource group was deleted. |
| Container App secret (JWT signing key) | `jwt-signing-key` | — | 64-byte base64 value generated locally with `openssl rand -base64 48`; referenced via `secretref:` from env var `Jwt__SigningKey`. Removed implicitly when the resource group was deleted. |
| Entra app (OIDC) | `github-cloudci-oidc-api` (display name `github-cloudci-api-oidc`) | tenant `6ee71fa2-3288-478d-a39f-fa453d0984f5` | AppId `cbe3d715-650e-44d6-9802-381870be8c63`. Granted `AcrPush` on the registry and `Container Apps Contributor` on the Container App. Deleted during the cleanup substep. |
| Federated credential | `main-branch` | — | Issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`, subject `repo:larsappel/cloudci-api:ref:refs/heads/main`. |

## Live URL

`https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/`

The visible cue progresses through the three exercises:

- **After Ex 1** — `GET /swagger` renders three operations under **Quotes**; `GET /api/quotes` returns the seed JSON array anonymously.
- **After Ex 2** — `GET /api/quotes` without `X-Api-Key` returns `401` with `WWW-Authenticate: ApiKey`; with the right key it still returns the array. Swagger UI's **Authorize** button accepts the API key.
- **After Ex 3** — `GET /api/quotes` requires `Authorization: Bearer <jwt>`; tokens are minted by `POST /api/tokens/login` with username/password against an in-memory user store. Swagger UI's **Authorize** button accepts a bearer token. Tampered, expired, and malformed tokens all return `401`.

The FQDN itself is unreachable after the cleanup substep (`az group delete -n rg-api-week6 --yes --no-wait`).

## GitHub Repository

- Repo: <https://github.com/larsappel/cloudci-api>
- Final secret list (after Ex 6.3, **before** any optional `gh secret delete`): `ACR_NAME`, `AZURE_CLIENT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`. After cleanup these values are stale and inert — the Entra app they point at no longer exists.
- Final workflow file: `.github/workflows/ci.yml` — federates with Azure via OIDC, builds the Docker image with the commit SHA as the tag, pushes to ACR, calls `az containerapp update --image`, and runs a 20-attempt smoke test against `/swagger/index.html`.

## Workflow Run History (validating each exercise)

| Stage | Commit | Run ID | Outcome |
|-------|--------|--------|---------|
| Ex 1 — initial OIDC pipeline + first deploy | `e5ae6bf` | [25046853642](https://github.com/larsappel/cloudci-api/actions/runs/25046853642) | success (5m 6s) |
| Ex 1 — App Insights SDK landed | `e8dc3de` | [25047128141](https://github.com/larsappel/cloudci-api/actions/runs/25047128141) | success |
| Ex 2 — API-key middleware + Swagger Authorize | `f95fb70` | [25047379474](https://github.com/larsappel/cloudci-api/actions/runs/25047379474) | success |
| Ex 2 — smoke test scoped to `/swagger/index.html` (follow-up) | `570414f` | [25047438587](https://github.com/larsappel/cloudci-api/actions/runs/25047438587) | success |
| Ex 3 — JWT bearer replaces API key + `TokensController` | `cc69daa` | [25047525701](https://github.com/larsappel/cloudci-api/actions/runs/25047525701) | success |

The cleanup substep ran **after** the last workflow finished and is not itself a CI run.

## Build SHA Progression

The deployed image tag changes per push because the workflow tags as `${{ github.sha }}`. The visible cue is more pedagogical than visual:

| After exercise | Visible cue | Source |
|----------------|-------------|--------|
| Ex 1 | `/api/quotes` returns the four-item seed array **anonymously**; `/swagger` shows three operations under **Quotes**. | Pipeline run `25046853642` (commit `e5ae6bf`). |
| Ex 2 | `/api/quotes` without `X-Api-Key` returns `401` + `WWW-Authenticate: ApiKey`; with the right key returns the seed array. Swagger UI shows an API-key Authorize dialog. | Pipeline run `25047438587` (commit `570414f` — the smoke-test follow-up was the active revision when Ex 2 verification was captured). |
| Ex 3 (final) | `/api/quotes` without bearer returns `401`; `POST /api/tokens/login` with valid creds returns a JWT; `GET /api/quotes` with `Authorization: Bearer <token>` returns the seed array. Swagger UI shows a Bearer Authorize dialog. The Tokens tag exposes `POST /api/tokens/login`. | Pipeline run `25047525701` (commit `cc69daa`). |

## New Endpoints

- `https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/api/quotes` — quotes resource. Status transitioned across exercises: anonymous→`200` in Ex 1; `401`/`403` without key, `200` with key in Ex 2; `401` without `Authorization: Bearer`, `200` with valid bearer in Ex 3.
- `https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/api/tokens/login` — POST credentials, returns `{"token": "<jwt>", "expiresAt": "<iso8601>"}` for valid credentials, `401` otherwise. Anonymous, by design.
- `https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/swagger` — Swagger UI, always anonymous (the chapter's deliberate trade-off, documented in the Concept Deep Dive of Ex 6.1 Step 5).

## Validation Artifacts

- **curl matrix** — `docs/validation/week-6-curl-matrix.txt`. Two sections: Section B captured during the Ex 6.2 state (anonymous → `401` + `WWW-Authenticate: ApiKey`; wrong key → `403`; right key → `200`); Section C captured during the Ex 6.3 state (anonymous, valid login, bad creds, valid bearer, tampered token, expired token, malformed token, plus a base64-decoded view of the JWT payload showing the `sub`, `name`, `role`, `exp`, `iss`, `aud` claims).
- **Swagger screenshot** — `docs/screenshots/week-6-swagger.png`. The deployed `/swagger` page after Ex 6.3, showing the three Quotes operations, the Tokens login operation, the Authorize button bound to the Bearer scheme, and the `CreateQuoteRequest` / `LoginRequest` / `QuoteDto` schemas.
- **Authorized call screenshot** — `docs/screenshots/week-6-authorised-call.png`. The Swagger `GET /api/Quotes` operation expanded with a 200 response after the Authorize flow accepted a JWT.
- **App Insights query transcript** — `docs/validation/week-6-app-insights-output.txt`. Four KQL queries against `cloudci-api-insights`. Q1 shows 27× `GET api/Quotes 200` (post-JWT path), 19× `GET /api/quotes 401` (anonymous bounces from validation), 4× `/swagger 200` (Playwright + manual visits), 3× successful `POST Tokens/Login`, 2× `403` (Ex 6.2 wrong-key tests), and a handful of static asset hits. Q2 confirms zero exceptions in the JWT-only path. Q3 enumerates all failures by name and resultCode. Q4 lists the most recent 5 requests with operation IDs, proving correlation IDs flow through.

## Manual Verification Steps

> The chapter's cleanup substep already deleted `rg-api-week6` and the Entra app `github-cloudci-api-oidc`, so steps 1–7 below describe the **pre-cleanup** verification. They are reproducible by re-running the chapter end-to-end against a fresh resource group; the names below are the values used in this validation run.

1. **Repository overview**

    ```bash
    gh repo view larsappel/cloudci-api --web
    ```

2. **Swagger UI reachable (anonymous)**

    ```bash
    curl -I https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/swagger/index.html
    ```

    Expected: `HTTP/2 200`.

3. **Quotes endpoint gated by JWT after Ex 3**

    ```bash
    curl -i https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/api/quotes
    ```

    Expected: `HTTP/2 401` and `WWW-Authenticate: Bearer`.

4. **Acquire a JWT**

    ```bash
    curl -s -X POST https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/api/tokens/login \
      -H "Content-Type: application/json" \
      -d '{"username":"alice","password":"alice123"}'
    ```

    Expected: a JSON object with a `token` field (a 388-char three-segment string) and `expiresAt` ~1 hour ahead.

5. **Quotes endpoint with bearer token**

    ```bash
    TOKEN=<paste-token>
    curl -s https://ca-api-week6.politeplant-6df3bf1f.northeurope.azurecontainerapps.io/api/quotes \
      -H "Authorization: Bearer $TOKEN"
    ```

    Expected: the seed JSON array of four quotes.

6. **No client secret in GitHub**

    ```bash
    gh secret list --repo larsappel/cloudci-api
    ```

    Expected: `ACR_NAME`, `AZURE_CLIENT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`. No `AZURE_CREDENTIALS`.

7. **Container App secrets present (pre-cleanup)**

    ```bash
    az containerapp secret list -g rg-api-week6 -n ca-api-week6 -o table
    ```

    Expected: `appinsights-connstr`, `api-key`, `jwt-signing-key`.

8. **Cleanup — verify post-state**

    ```bash
    az group exists -n rg-api-week6
    az ad app list --display-name github-cloudci-api-oidc -o tsv
    ```

    Expected: `false` (after the async delete completes, typically 3–5 minutes); empty output (no rows).

## Deviations from Exercise Text

1. **Swashbuckle 10.x and Microsoft.OpenApi 2.x dropped the `Microsoft.OpenApi.Models` namespace.** Running `dotnet add package Swashbuckle.AspNetCore` (no version pin) on .NET 10 resolved Swashbuckle 10.1.7, which transitively pulls Microsoft.OpenApi 2.0.0 — and the new package consolidates the old `Microsoft.OpenApi.Models.*` types under the root `Microsoft.OpenApi` namespace. The exercise text writes `using Microsoft.OpenApi.Models;`. Fix used here: pinned Swashbuckle to `6.6.2` (which uses Microsoft.OpenApi 1.6.x with the legacy `.Models` namespace) AND removed the auto-included `Microsoft.AspNetCore.OpenApi 10.0.3` package, which was bringing the 2.x line transitively. Both changes belong in Ex 6.1 Step 5 — either pin Swashbuckle and explain why, or migrate the snippets to the `Microsoft.OpenApi` (no `.Models`) namespace and pin to a 10.x line that ships consistently.
2. **`Microsoft.ApplicationInsights.AspNetCore` 3.1.0 fails to start without a connection string.** The unpinned `dotnet add package` call resolved 3.1.0, the new OpenTelemetry-backed line. Unlike the 2.x SDK, 3.x throws `System.InvalidOperationException: A connection string was not found. Please set your connection string.` at host startup if `APPLICATIONINSIGHTS_CONNECTION_STRING` is unset — which makes Ex 6.2 Step 4's local `dotnet run` impossible without setting a fake env var first. Fix used here: pinned the package to `2.22.0` for parity with the silent-no-op behavior the exercise assumes. The same pin was applied in the Week 5 reference project for the same reason. Either pin the package in the exercise text, or update the local-run instructions to either set a placeholder env var or comment out the registration during local development.
3. **`Microsoft.AspNetCore.OpenApi` 10.0.3 is included by the `dotnet new webapi` template by default.** It conflicts with Swashbuckle 6.x by forcing the Microsoft.OpenApi 2.x line. Removing it (`dotnet remove package Microsoft.AspNetCore.OpenApi`) is the cleaner path — the exercise uses Swashbuckle, not the built-in OpenAPI generator. Worth a callout in Ex 6.1 Step 1 ("the scaffold ships an OpenAPI package you don't need; remove it before adding Swashbuckle").
4. **Smoke test scope.** Ex 6.1's workflow probes `/api/quotes` to validate the deploy, which works in Ex 6.1's anonymous state but starts failing the moment Ex 6.2 lands the API-key middleware. Ex 6.2 Step 9 mentions this explicitly; the live-execution fix used here was a follow-up commit (`570414f`) that scopes the smoke test to `/swagger/index.html`, which is always anonymous. The exercise text could either bake this into the Ex 6.1 workflow from the start, or surface it as part of Ex 6.2 Step 9 with a concrete diff.
5. **Application Map screenshot deferred.** The Azure Portal blade for Application Map requires interactive Microsoft SSO; capturing it programmatically is impractical. Same deviation as Week 5. The KQL query transcripts in `docs/validation/week-6-app-insights-output.txt` prove telemetry is flowing — that is the load-bearing evidence.
6. **JWT claim names rendered as Microsoft schema URIs.** The decoded payload of an issued token shows `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name` and `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` instead of the JWT-canonical short names `name` and `role`. This is `JwtSecurityTokenHandler`'s default behavior when claims are constructed with `ClaimTypes.Name`/`ClaimTypes.Role` rather than `JwtRegisteredClaimNames.Name` (which doesn't exist for `name`) or raw strings. The token still validates and `[Authorize]` works correctly. The exercise text's Concept Deep Dive on JWT shows the short forms (`sub`, `name`, `role`); a one-line note explaining that `ClaimTypes.*` constants emit the long URI form would close the gap.

## Status

**Validated end-to-end on 2026-04-28.** All five workflow runs green; live curl matrix matches the exercise text's promised end-states; Swagger UI captures match what the chapter promises; App Insights ingested representative traffic across all three states; cleanup substep executed and resource group queued for asynchronous deletion. The chapter is ready to teach.
