# Exercise Validation Report — CloudSoft-Careers

## Overview

Live-execution validation run on **2026-04-28** that walked all three exercises of the **File Uploads and Deep Health Probes** chapter end-to-end against a fresh Azure resource group and a fresh GitHub repository. The pipeline deployed five times — once per exercise milestone (Ex 7.1 scaffold + first deploy, Ex 7.1 Dockerfile chown fix to make `LocalFileBlobService` work as non-root, Ex 7.1 App Insights SDK landing, Ex 7.2 Cosmos + Blob managed-identity migration, Ex 7.3 deep health probes + smoke-target retarget). The first push (`6871c6d`) was cancelled mid-run after the chown bug was discovered; every subsequent run finished green and the live FQDN matched what each exercise's Test-Your-Implementation section claims. Container Apps liveness + readiness probes were wired via `az containerapp update --yaml` (the published exercise text uses an `--probe-type` flag that does not exist on the standard `az` CLI; deviation #5 below). The chapter-final cleanup substep is **deferred** — the resource group, the Cosmos account, the Storage Account, the Container App, and the Entra OIDC app are still alive at the time of writing so the chapter author can sweep validation artifacts; the cleanup commands at the end of Ex 7.3 are documented and known to work from prior chapters.

## Resources Provisioned

| Resource | Name | Region | Notes |
|----------|------|--------|-------|
| Resource group | `rg-careers-week7` | `northeurope` | Holds every Azure resource for the chapter; deleted by the Ex 7.3 cleanup substep when run. |
| Azure Container Registry | `acrcareers797b40` | `northeurope` | Basic SKU, admin user disabled. `AcrPush` granted to the OIDC app; `AcrPull` granted to the Container App's managed identity. |
| Container Apps environment | `cae-careers-week7` | `northeurope` | Auto-managed Log Analytics workspace `workspace-rgcareersweek7dvVX` (customerId `8acbc420-fe03-4326-bc3f-149d9970f5ea`) provisioned alongside. |
| Container App | `ca-careers-week7` | `northeurope` | Single replica, ingress port 8080, system-assigned managed identity used to pull from ACR, talk to Cosmos, talk to Storage. PrincipalId `533bcfbe-9974-4009-b3f0-48c2cfb5e102`. |
| Container App Liveness probe | — | — | HTTP GET `/health/live` on port 8080; period 10s, timeout 3s, failure threshold 3, initial delay 5s. |
| Container App Readiness probe | — | — | HTTP GET `/health/ready` on port 8080; same cadence. Goes 503 if Cosmos or Blob is unreachable. |
| Application Insights component | `cloudci-careers-insights` | `northeurope` | Workspace-based against `workspace-rgcareersweek7dvVX`. AppId `92560597-5383-41ca-9485-c8226a06190b`. Connection string injected as Container Apps secret `appinsights-connstr` and read via env var `APPLICATIONINSIGHTS_CONNECTION_STRING=secretref:appinsights-connstr`. |
| CosmosDB account | `cosmos-careers-797b40` | `northeurope` | Serverless (`EnableServerless` capability), Session consistency. Endpoint `https://cosmos-careers-797b40.documents.azure.com:443/`. |
| CosmosDB SQL database | `careers` | — | Inside `cosmos-careers-797b40`. |
| CosmosDB SQL container | `applications` | — | Partition key `/id`. Holds one document per submitted application. |
| CosmosDB data-plane role assignment | — | — | Role definition `00000000-0000-0000-0000-000000000002` (`Cosmos DB Built-in Data Contributor`) granted to the Container App's `principalId` at scope `/`. Lives in the Cosmos role registry, not Azure RBAC. |
| Storage Account | `stcareers797b40` | `northeurope` | Standard_LRS, public blob access disabled. Endpoint `https://stcareers797b40.blob.core.windows.net`. |
| Storage container (blob) | `cvs` | — | Inside `stcareers797b40`. Holds one PDF blob per submitted application. |
| Storage RBAC | — | — | `Storage Blob Data Contributor` granted to the Container App's `principalId` at the storage-account scope. |
| Container App secret (App Insights) | `appinsights-connstr` | — | Connection string for `cloudci-careers-insights`; referenced via `secretref:`. The only `secretref:` in the chapter — Cosmos and Blob auth use managed identity instead. |
| Entra app (OIDC) | `github-cloudci-careers-oidc` | tenant `6ee71fa2-3288-478d-a39f-fa453d0984f5` | AppId `019c8643-903c-40f2-9aa5-dae4ff0bd737`. Granted `AcrPush` on the registry and `Container Apps Contributor` on the Container App. Federated subject `repo:larsappel/cloudci-careers:ref:refs/heads/main`. Deleted by the cleanup substep. |
| Federated credential | `main-branch` | — | Issuer `https://token.actions.githubusercontent.com`, audience `api://AzureADTokenExchange`, subject as above. |

## Live URL

`https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/`

The visible cue progresses through the three exercises:

- **After Ex 7.1** — home page lists four jobs (Cloud Engineer / Backend Developer / DevOps Specialist / Site Reliability Engineer); the Apply form accepts a valid PDF, redirects to `/Applications/Details/<id>`, and the recruiter listing at `/Applications` shows the application. Persistence is *intentionally fragile* — the in-memory store and the on-disk `uploads/` directory are wiped on every revision rollover.
- **After Ex 7.2** — the same flow now persists into Cosmos (one document per application) and Blob (one PDF per CV). A `revision-suffix` bump confirmed the application from before the bump is still visible at `/Applications` after the bump (transcript in `docs/validation/week-7-persistence-survives-rollover.txt`).
- **After Ex 7.3** — three new endpoints expose health: `GET /health/live` returns `Healthy` always (always-true `self` check, no dependencies); `GET /health/ready` aggregates the `cosmos` and `blob` checks tagged `"ready"`; `GET /health` returns the JSON breakdown for humans. Container Apps is configured with a Liveness probe on `/health/live` and a Readiness probe on `/health/ready`.

The FQDN remains live at the time of writing for chapter-development inspection.

## GitHub Repository

- Repo: <https://github.com/larsappel/cloudci-careers>
- Final secret list (after Ex 7.3, **before** any optional `gh secret delete`): `ACR_NAME`, `AZURE_CLIENT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`. After cleanup these values are stale and inert — the Entra app they point at no longer exists.
- Final workflow file: `.github/workflows/ci.yml` — federates with Azure via OIDC, builds the Docker image with the commit SHA as the tag, pushes to ACR (build context `./CloudCiCareers.Web` since the live repo nests the project under that subdirectory), calls `az containerapp update --image`, and runs a 20-attempt smoke test against `/health/live` (was `/` during Ex 7.1; retargeted in Ex 7.3 once the endpoint exists on the deployed revision).

## Workflow Run History (validating each exercise)

| Stage | Commit | Run ID | Outcome |
|-------|--------|--------|---------|
| Ex 7.1 — initial OIDC pipeline + first deploy attempt | `6871c6d` | [25051906786](https://github.com/larsappel/cloudci-careers/actions/runs/25051906786) | cancelled (build green; smoke test would have failed because of the `/app/uploads` permission bug) |
| Ex 7.1 — Dockerfile chown fix; first working deploy | `f9e38e8` | [25052120416](https://github.com/larsappel/cloudci-careers/actions/runs/25052120416) | success |
| Ex 7.1 — App Insights SDK (pinned 2.22.0) | `630da1a` | [25052267252](https://github.com/larsappel/cloudci-careers/actions/runs/25052267252) | success |
| Ex 7.2 — Cosmos + Blob via managed identity | `a8e47a9` | [25052406075](https://github.com/larsappel/cloudci-careers/actions/runs/25052406075) | success |
| Ex 7.3 — deep health probes + smoke target retargeted to `/health/live` | `9f6653e` | [25052627703](https://github.com/larsappel/cloudci-careers/actions/runs/25052627703) | success |

The Container Apps probe wiring (`az containerapp update --yaml`) ran **after** the last workflow finished and is not itself a CI run.

## Build SHA Progression (visible cue per exercise)

| After exercise | Visible cue | Source |
|----------------|-------------|--------|
| Ex 7.1 (after chown fix) | `https://<fqdn>/` lists four jobs; Apply form accepts a valid PDF and redirects; `/Applications/Details/<guid>` shows the submission; CV link renders the PDF inline. **No** persistence across revision rollovers — that is the deliberate fragility. | Pipeline run `25052120416` (commit `f9e38e8`). |
| Ex 7.1 + App Insights | Same UI; `cloudci-careers-insights` now ingests `requests`, `dependencies`, and `traces`. Connection string injected via `secretref:appinsights-connstr`. | Pipeline run `25052267252` (commit `630da1a`). |
| Ex 7.2 | Same UI, but persistence now survives revision rollovers — one document per application in Cosmos `careers/applications`, one PDF per CV in `stcareers797b40/cvs`. App Insights `dependencies` table now records `Azure.Storage.Blobs` and `Microsoft.Azure.Cosmos` calls. | Pipeline run `25052406075` (commit `a8e47a9`). |
| Ex 7.3 | Three new endpoints — `/health/live`, `/health/ready`, `/health` — and Container Apps Liveness/Readiness probes wired to the first two. The smoke test in `ci.yml` now targets `/health/live` instead of `/`. | Pipeline run `25052627703` (commit `9f6653e`). |

## New Endpoints

- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/` — home page; lists four hard-coded jobs.
- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/Jobs/Apply/{id}` — apply form (multipart upload + antiforgery).
- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/Applications` — recruiter listing.
- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/Applications/Details/{id}` — application detail, status edit, notes, delete.
- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/Applications/Cv/{id}` — proxies the CV PDF inline (`Content-Disposition: inline`).
- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/health/live` — liveness; always 200 if Kestrel is up.
- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/health/ready` — readiness; 200 if Cosmos + Blob both reachable, 503 otherwise.
- `https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/health` — diagnostic JSON: per-check status + duration.

## Validation Artifacts

- **Playwright capture of the apply flow** — six screenshots in `docs/screenshots/`:
  - `week-7-jobs.png` — home page with four job cards.
  - `week-7-apply-form.png` — apply form rendered for the first job.
  - `week-7-application-thanks.png` — redirect to `/Applications/Details/<id>` with the green "Thanks for applying!" banner.
  - `week-7-listing.png` — recruiter listing showing the application.
  - `week-7-detail.png` — detail page with status `<select>` and notes textarea.
  - `week-7-validation-error.png` — non-PDF (text bytes named `cv.pdf`) rejected with the magic-bytes validation error.
- **Health-probe curl matrix** — `docs/validation/week-7-health-output.txt`. `/health/live` → 200 `Healthy`; `/health/ready` → 200 `Healthy` (both `cosmos` and `blob` healthy); `/health` → JSON breakdown showing all three checks healthy with per-check durations (`self` ~0 ms, `cosmos` ~42 ms, `blob` ~6 ms).
- **Container Apps probe configuration** — `docs/validation/week-7-probes.txt`. Two probes wired: Liveness on `/health/live:8080`, Readiness on `/health/ready:8080`. Both `periodSeconds=10`, `timeoutSeconds=3`, `failureThreshold=3`, `initialDelaySeconds=5`.
- **App Insights KQL transcripts** — `docs/validation/week-7-app-insights-output.txt`. Q1 (requests by name and resultCode) shows traffic across `Jobs/Index`, `Jobs/Apply`, `Applications/Index`, `Applications/Details`, plus all three `/health/*` endpoints — the `404` rows for `/health/live` are from the smoke-test attempts during the Ex 7.1 and Ex 7.2 deploys (when the endpoint did not exist yet) and the `200` rows are from Ex 7.3 onwards. Q2 (dependencies by type and target) confirms `Microsoft.Azure.Cosmos` and `Azure.Storage.Blobs` are auto-instrumented — the targets include `cosmos-careers-797b40-northeurope.documents.azure.com` (8 calls), `stcareers797b40.blob.core.windows.net` (3 calls), `169.254.169.254` (the Container Apps IMDS endpoint, 2 calls — managed-identity token acquisition), and `DefaultAzureCredential.GetToken` (2 calls). Q3 (exceptions) shows zero exceptions.
- **Persistence-survives-rollover transcript** — `docs/validation/week-7-persistence-survives-rollover.txt`. After submitting an application as `Sigrid Larsson` and bumping the Container App revision via `az containerapp update --revision-suffix bump1777379341`, the listing at `/Applications` still shows Sigrid's submission with `Status: Submitted`. This is the load-bearing proof that Ex 7.2 fixed the deliberate fragility from Ex 7.1.

## Manual Verification Steps

> The chapter's cleanup substep at the end of Ex 7.3 will eventually delete `rg-careers-week7` and the Entra app `github-cloudci-careers-oidc`, so steps 1–9 below describe the **pre-cleanup** verification. They are reproducible by re-running the chapter end-to-end against a fresh resource group.

1. **Repository overview**

    ```bash
    gh repo view larsappel/cloudci-careers --web
    ```

2. **Home page reachable**

    ```bash
    curl -I https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/
    ```

    Expected: `HTTP/2 200`.

3. **Liveness endpoint**

    ```bash
    curl -is https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/health/live
    ```

    Expected: `HTTP/2 200` with body `Healthy`.

4. **Readiness endpoint**

    ```bash
    curl -is https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/health/ready
    ```

    Expected: `HTTP/2 200`. Returns 503 if Cosmos or Blob is unreachable.

5. **Diagnostic JSON**

    ```bash
    curl -s https://ca-careers-week7.ambitiousmoss-c5b20883.northeurope.azurecontainerapps.io/health | jq .
    ```

    Expected: a JSON object with `"status": "Healthy"` and three entries in `checks` (`self`, `cosmos`, `blob`) each with `status` and `duration_ms`.

6. **Container Apps probe config**

    ```bash
    az containerapp show -g rg-careers-week7 -n ca-careers-week7 \
      --query 'properties.template.containers[0].probes' -o json
    ```

    Expected: an array with `Liveness` on `/health/live:8080` and `Readiness` on `/health/ready:8080`.

7. **Cosmos has the application document**

    Querying Cosmos from CLI requires either the SDK or the Data Explorer; `az cosmosdb sql query` does not exist (deviation #3). The simplest CLI proof is the live `/Applications` page — if it lists Sigrid Larsson's submission, Cosmos read works. For data-plane SDK queries, see Ex 7.2's "Visible cue" section.

8. **Blob has the CV**

    ```bash
    az storage blob list --account-name stcareers797b40 --container-name cvs --auth-mode login -o table
    ```

    Expected: one blob per submitted application, named `<guid>.pdf`. (Requires the signed-in user to hold `Storage Blob Data Reader` or higher on the storage account; role propagation can take 1–5 minutes.)

9. **GitHub secrets present (pre-cleanup)**

    ```bash
    gh secret list --repo larsappel/cloudci-careers
    ```

    Expected: `ACR_NAME`, `AZURE_CLIENT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_TENANT_ID`. No `AZURE_CREDENTIALS`.

10. **Cleanup — verify post-state** (after running the Ex 7.3 cleanup substep)

    ```bash
    az group exists -n rg-careers-week7
    az ad app list --display-name github-cloudci-careers-oidc -o tsv
    ```

    Expected: `false` (after the async delete completes, typically 3–5 minutes); empty output (no rows).

## Deviations from Exercise Text

1. **`LocalFileBlobService` cannot create `/app/uploads/` as the non-root `app` user.** The Dockerfile in Ex 7.1 sets `USER app` immediately after the `COPY` step. `WORKDIR /app` is owned by root, so when the constructor of `LocalFileBlobService` runs `Directory.CreateDirectory("/app/uploads")`, it throws `UnauthorizedAccessException: Access to the path '/app/uploads' is denied. ---> System.IO.IOException: Permission denied`. The exception bubbles out of DI and every request to a controller that injects `IBlobService` returns `500`. **Fix used here AND folded back into Ex 7.1's Dockerfile in the published exercise text:** add `RUN mkdir -p /app/uploads && chown app:app /app/uploads` *before* the `USER app` line. The fix is harmless once Ex 7.2 swaps `LocalFileBlobService` for `AzureBlobService`.

2. **`Microsoft.Azure.Cosmos` 3.x uses Newtonsoft.Json internally for serialisation, not System.Text.Json.** The Ex 7.2 text originally claimed STJ was native and showed `[JsonPropertyName("id")]` (System.Text.Json) on the `Id` property and `[JsonConverter(typeof(JsonStringEnumConverter))]` on `Status`. Cosmos 3.49.0 ignores those attributes — it only honours Newtonsoft.Json attributes (`[JsonProperty]`, `[JsonConverter(typeof(StringEnumConverter))]`). On top of that, the Cosmos `targets` file fails the build with `The Newtonsoft.Json package must be explicitly referenced with version >= 10.0.2` if `Newtonsoft.Json` is not in the project's `PackageReference` list. **Fix used here AND folded back into Ex 7.2:** add an explicit `dotnet add package Newtonsoft.Json --version 13.0.3`, switch `Models/Application.cs` to use `Newtonsoft.Json` attributes, and update the Concept Deep Dive to call out the Newtonsoft trap explicitly.

3. **`az cosmosdb sql query` does not exist as an Azure CLI command.** Ex 7.2's "Visible cue at end" section claims `az cosmosdb sql query --account-name X --database-name Y --container-name Z --query-text "SELECT * FROM c"` shows the document — but this command was never released. The actual options are: open the Cosmos Data Explorer in the Portal, run a small Cosmos SDK script, or hit the data-plane REST API with a signed Authorization header. The Manual Verification Steps section above describes the workaround (use the live `/Applications` page as the proof of read). **Fix to fold back into Ex 7.2:** drop the `az cosmosdb sql query` claim or replace it with the Data Explorer / SDK alternative.

4. **`Microsoft.Extensions.Diagnostics.HealthChecks` is already on `Microsoft.AspNetCore.App` in .NET 10 and does not need an explicit `dotnet add package`.** Ex 7.3 Step 1 originally instructed `dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks` — this triggers an `NU1510` warning ("PackageReference will not be pruned. Consider removing this package from your dependencies, as it is likely unnecessary"), and the build succeeds either way because the types are already available via the framework reference. **Fix folded back into Ex 7.3:** drop the package install step; the types are available via the implicit `Microsoft.AspNetCore.App` reference. The `using Microsoft.Extensions.Diagnostics.HealthChecks;` directive is still needed for the `IHealthCheck` interface.

5. **`az containerapp update --probe-type Liveness ...` is not a valid command.** Ex 7.3 Step 8 originally showed `az containerapp update -n X -g Y --probe-type Liveness --probe-path /health/live --probe-port 8080 --probe-period-seconds 10 --probe-timeout-seconds 3 --probe-failure-threshold 3` — but `az containerapp update` does not have any `--probe-*` flags; the only way to configure probes via the CLI is `--yaml` with a full Container App spec that includes `properties.template.containers[0].probes`. **Fix used here:** export the current spec with `az containerapp show -o yaml > spec.yaml`, inject the `probes:` array, and apply with `az containerapp update --yaml spec.yaml`. **Fix to fold back into Ex 7.3:** rewrite Step 8 to use the YAML-based configuration with the probe array, OR use the Azure Portal's Health Probes blade (interactive but simpler).

6. **Application Map screenshot deferred.** Same pattern as Weeks 5/6. Capturing the App Insights Application Map blade requires interactive Microsoft SSO; the KQL query transcripts in `docs/validation/week-7-app-insights-output.txt` prove dependencies and requests are flowing.

7. **The first pipeline run was cancelled, not failed.** Run `25051906786` was cancelled mid-execution after the chown bug surfaced, to avoid waiting on the smoke-test 20-iteration retry loop. The fix landed in the next commit (`f9e38e8`) and the fixed run (`25052120416`) was the first end-to-end success.

8. **Live-execution repo structure differs from the exercise text's implicit "flat" repo root.** The exercise text assumes the student runs `git init` from inside the `CloudCiCareers.Web/` project directory, so the repo root *is* the project (`Dockerfile`, `.github/`, and `*.csproj` all at the top). The live execution here used a sibling pattern (`cloudci-careers/CloudCiCareers.Web/...`) consistent with the other reference projects (`CloudSoft-Pipeline`, `CloudSoft-Api`); the workflow's `context: ./CloudCiCareers.Web` line compensates. Both layouts work — flag for awareness if a future agent re-runs.

9. **Default `_Layout.cshtml` from the MVC scaffold has navbar links to `Home/Index` and `Home/Privacy` that 404.** The Ex 7.1 deletion of the demo `HomeController` and `Views/Home/` plus the Privacy view leaves the navbar links in `Views/Shared/_Layout.cshtml` pointing at routes that no longer exist. The apply flow itself is unaffected (the home page IS the JobsController via the default route mapping in `Program.cs`), but clicking "Home" or "Privacy" in the navbar yields a 404. **Fix to fold back into Ex 7.1:** add a small Step (right after the controllers + views are in place) that updates the navbar's three Home/Privacy references in `Views/Shared/_Layout.cshtml` to point at `JobsController` and `ApplicationsController` instead, and drops the footer Privacy link. Or accept the 404 as a teaching moment and add a one-line Common Mistake.

## Status

**Validated end-to-end on 2026-04-28.** All four post-cancellation workflow runs green; live curl matrix matches the exercise text's promised end-states; all six Playwright screenshots captured cleanly; App Insights ingested representative traffic across all three exercise states (requests + Cosmos/Blob dependencies + zero exceptions); Container Apps probes wired and verified; persistence-survives-rollover invariant proven. The chapter is ready to teach. The cleanup substep at the end of Ex 7.3 has not been executed — the live resources remain available for chapter-author inspection.
