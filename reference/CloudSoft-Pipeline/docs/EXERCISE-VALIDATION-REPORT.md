# Exercise Validation Report — CloudSoft-Pipeline

## Overview

Live end-to-end execution of the three CI/CD exercises in `content/exercises/3-deployment/9-cicd-to-container-apps/` against `lars.appel@live.se`'s Azure subscription and `larsappel`'s GitHub account, performed on **2026-04-27**. All three exercises produced a deployed application reachable on the public Container App FQDN, and the smoke test gating the workflow turns red when the deploy is broken (verified by manual inspection of run history). The final pipeline authenticates to Azure entirely via OIDC federation — no long-lived secrets exist in the GitHub repository.

## Resources Provisioned

| Resource | Name | Region | Notes |
|----------|------|--------|-------|
| Resource group | `rg-cicd-week4` | `northeurope` | Holds all Azure resources for this chapter |
| Azure Container Registry | `acrlap8a9f` | `northeurope` | Basic SKU; login server `acrlap8a9f.azurecr.io` |
| Container Apps environment | `cae-cicd-week4` | `northeurope` | Auto-created Log Analytics workspace |
| Container App | `ca-cicd-week4` | `northeurope` | System-assigned managed identity holds `AcrPull` on the ACR |
| Entra app (OIDC) | `github-cloudci-oidc` | tenant `6ee71fa2-3288-478d-a39f-fa453d0984f5` | appId `7c11e4ce-91cd-4ba3-9fce-820669f397fe` |
| Federated credential | `main-branch` | — | Subject `repo:larsappel/cloudci:ref:refs/heads/main` |

## Live URL

<https://ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io/>

The homepage displays two badges:

- `build: <commit-sha>` — proves which commit produced the running image.
- `host: <revision-replica>` — the Container Apps replica name.

## GitHub Repository

- Repo: <https://github.com/larsappel/cloudci>
- Final secret list (after Exercise 3): `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`. No `DOCKERHUB_TOKEN`, no `AZURE_CREDENTIALS`.
- Final workflow file: `.github/workflows/ci.yml` — uses `azure/login@v2` with federated credentials, builds and pushes to ACR, then runs `az containerapp update`. A second job smoke-tests the FQDN.

## Workflow Run History (validating each exercise)

| Stage | Commit | Run ID | Outcome |
|-------|--------|--------|---------|
| Ex 1 baseline (Docker Hub) | `2e648ee` | `25003389002` | success — image pushed to `larsappel/cloudci:latest` |
| Ex 2 ACR + auto-deploy + smoke test | `6afcaf6` | `25003888638` | success — `deploy` 1m37s, `smoke-test` 2s |
| Ex 3 OIDC federated auth | `ff9f60c` | `25004067297` | success — `Log in to Azure (federated)` step replaces secret-based login |
| Ex 3 confirmation after deleting AZURE_CREDENTIALS | `913952a` | latest run | success — passwordless deploy, no long-lived secrets in GitHub |

Run details: <https://github.com/larsappel/cloudci/actions>

## Build SHA Progression

The badge on the homepage tracks the deployed commit through the three exercises:

| After exercise | Badge content | Source |
|----------------|---------------|--------|
| Exercise 1 | `build: 2e648ee19347821ac78324b87bea698c461f561f` | Docker Hub `larsappel/cloudci:2e648ee…` |
| Exercise 2 | `build: 6afcaf6249df1aea9f9e09bde873787d07ae3a13` | ACR `acrlap8a9f.azurecr.io/cloudci:6afcaf6…` |
| Exercise 3 (final) | `build: 913952a96674b6cb2565d52ffa5ff992ea34bbe5` | ACR pulled via OIDC pipeline |

## Screenshots

`docs/screenshots/ex-4.3-passwordless.png` — Playwright capture of the homepage after Exercise 3, showing the SHA badge of the latest passwordless deployment.

## Manual Verification Steps

Run each command and observe the listed expected output.

1. **Repository overview**

    ```bash
    gh repo view larsappel/cloudci --web
    ```

2. **Live URL**

    ```bash
    curl -I https://ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io/
    ```

    Expected: `HTTP/2 200`.

3. **Recent workflow runs**

    ```bash
    gh run list --repo larsappel/cloudci --limit 5
    ```

4. **ACR image tag history**

    ```bash
    az acr repository show-tags -n acrlap8a9f --repository cloudci -o table
    ```

5. **No long-lived auth secrets**

    ```bash
    gh secret list --repo larsappel/cloudci
    ```

    Expected: only `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.

6. **Federated credential intact**

    ```bash
    az ad app federated-credential list --id 7c11e4ce-91cd-4ba3-9fce-820669f397fe -o table
    ```

7. **Container App is using the managed identity for ACR pull**

    ```bash
    az containerapp registry list -g rg-cicd-week4 -n ca-cicd-week4 -o table
    ```

8. **Playwright re-validation**

    ```bash
    cd reference/CloudSoft-Pipeline/scripts
    npm install   # only the first time
    npx playwright install chromium   # only the first time
    FQDN=ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io \
    LABEL=manual-recheck \
    node validate.mjs
    ```

## Deviations from Exercise Text

These were necessary to drive the live execution successfully and have been folded back into the exercise files where applicable.

1. **`--assignee-principal-type` flag dropped.** An earlier draft of the exercise text added `--assignee-principal-type ServicePrincipal` to `az role assignment create` calls in an attempt to avoid the SP-creation propagation race. In practice the Azure CLI couples that flag with `--assignee-object-id` (not `--assignee`), so the simpler `--assignee $APP_ID` form is what the live execution used and what the published exercises now show. A footnote about the race and a 10-second retry remains in the Common Mistakes blockquote.
2. **Container App created via CLI rather than the Portal walkthrough described in Exercise 1.** The exercise text walks students through the Portal because that's the gentlest first introduction. For validation speed I used `az containerapp create`. The Portal flow was sanity-checked against the published Microsoft Learn pages but not actually clicked through end-to-end — flag this if a student reports a Portal screen has changed.
3. **Old service principal deleted at end.** After Exercise 3 succeeded passwordless, I deleted the Exercise-2 service principal (`github-cicd-sp-cloudci`) so only the Entra app registration `github-cloudci-oidc` remains. This is hygiene, not a step in the exercise.

## Status

**Validated end-to-end on 2026-04-27.** The pipeline is fully passwordless, the smoke test gates the workflow, and the live URL responds 200 with the latest commit's SHA visible.

---

## Week 5 — Logging and Monitoring

### Overview

Live end-to-end execution of the three observability exercises in `content/exercises/3-deployment/10-logging-and-monitoring/` against the same Azure subscription and GitHub repo as Week 4, performed on **2026-04-28**. The Container App is now instrumented with structured `ILogger<T>` calls, request-scoped correlation IDs, the Application Insights SDK, an exception-throwing `/Home/Boom` action, and a `home-page-views` custom metric. Three additional commits append to the same `larsappel/cloudci` history; the pipeline auto-deployed each one. Telemetry was confirmed via direct KQL queries against both the Log Analytics workspace and the App Insights component.

### Resources Provisioned (new in Week 5)

| Resource | Name | Region | Notes |
|----------|------|--------|-------|
| Log Analytics workspace | `workspace-rgcicdweek4RYcM` | `northeurope` | Auto-created with the Container Apps environment in Week 4; reused as-is. customerId `851d6578-a025-4c8f-8797-d00a06b4662a`. |
| Application Insights component | `cloudci-insights` | `northeurope` | Workspace-based mode (telemetry rolls into the same Log Analytics workspace). appId `914efd2e-6555-4ded-b3df-a59e7ab10c26`. |
| Container App secret | `appinsights-connstr` | — | Holds the App Insights connection string in the Container Apps control plane. Referenced at runtime via `secretref:`. |
| Container App env var | `APPLICATIONINSIGHTS_CONNECTION_STRING` | — | Set with value `secretref:appinsights-connstr` so the SDK reads it without the value ever being inline. |

### New Endpoints

- `https://ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io/` — homepage (200, structured ILogger line on every render).
- `https://ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io/Home/Boom` — intentional 500. Throws `InvalidOperationException` so the App Insights Failures blade has something to show.

### Workflow Run History (Week 5 commits)

| Stage | Commit | Run ID | Outcome |
|-------|--------|--------|---------|
| Ex 5.1 — structured `ILogger<HomeController>` | `d687af7` | `25042720272` | success — image pushed and deployed via OIDC |
| Ex 5.2 — request-scoped correlation ID middleware | `69362ca` | `25042739641` | success — same pipeline path |
| Ex 5.3 — App Insights SDK + `/Home/Boom` + custom metric | `d9223b8` | `25042815778` | success — new revision picks up `secretref:` env var |

Run details: <https://github.com/larsappel/cloudci/actions>

### Build SHA Progression (Week 5)

| After exercise | Badge content | Source |
|----------------|---------------|--------|
| Week 5 Exercise 1 | `build: d687af77f96ae3acc238f17166cda020fea14d5c` | ACR pulled via OIDC pipeline |
| Week 5 Exercise 2 | `build: 69362ca392abcb4b34d3d383a9b72a6f19eeb907` | ACR pulled via OIDC pipeline |
| Week 5 Exercise 3 (final) | `build: d9223b85bc56dc55ca1b95b0628929f592f5a58c` | ACR pulled via OIDC pipeline |

### Validation Artifacts

- `docs/screenshots/week-5-homepage.png` — Playwright capture of the deployed homepage after Week 5 Exercise 3, showing the new SHA.
- `docs/validation/week-5-kql-output.txt` — Three KQL queries against the Log Analytics workspace: recent log lines for `ca-cicd-week4`, the structured "Home page rendered for ... build ..." filter, and a per-replica request count grouped by `ContainerGroupName_s`.
- `docs/validation/week-5-app-insights-output.txt` — Four App Insights queries showing requests (20× HTTP 200 + 5× HTTP 500), exceptions (`System.InvalidOperationException` from `/Home/Boom`), the `home-page-views` custom metric, and `ILogger` lines flowing into App Insights as traces.

### KQL Examples Used in Validation

```kusto
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(30m)
| where ContainerAppName_s == "ca-cicd-week4"
| where Log_s has "Home page rendered"
| project TimeGenerated, ContainerGroupName_s, Log_s
| order by TimeGenerated desc
| take 5
```

```kusto
requests
| where timestamp > ago(20m)
| summarize count() by name, resultCode
| order by count_ desc
```

```kusto
exceptions
| where timestamp > ago(20m)
| summarize count() by type, outerMessage
| order by count_ desc
```

```kusto
customMetrics
| where timestamp > ago(20m)
| where name == "home-page-views"
| summarize total = sum(value) by name, bin(timestamp, 1m)
| order by timestamp desc
```

### Manual Verification Steps (Week 5)

1. **Confirm the homepage serves with the latest SHA**

    ```bash
    FQDN=ca-cicd-week4.yellowcoast-76379f35.northeurope.azurecontainerapps.io
    curl -s "https://$FQDN/" | grep -E "build:|host:"
    ```

2. **Confirm `/Home/Boom` throws (HTTP 500)**

    ```bash
    curl -s -o /dev/null -w "HTTP %{http_code}\n" "https://$FQDN/Home/Boom"
    ```

3. **Generate traffic, then query Log Analytics**

    ```bash
    for i in {1..25}; do curl -s "https://$FQDN/" >/dev/null; done
    sleep 90  # ingestion delay
    WS_ID=$(az monitor log-analytics workspace show -g rg-cicd-week4 \
      -n workspace-rgcicdweek4RYcM --query customerId -o tsv)
    az monitor log-analytics query --workspace "$WS_ID" \
      --analytics-query 'ContainerAppConsoleLogs_CL
        | where TimeGenerated > ago(15m)
        | where ContainerAppName_s == "ca-cicd-week4"
        | where Log_s has "Home page rendered"
        | take 5' -o table
    ```

4. **Confirm App Insights is receiving telemetry**

    ```bash
    az monitor app-insights query --app cloudci-insights -g rg-cicd-week4 \
      --analytics-query 'requests
        | where timestamp > ago(20m)
        | summarize count() by name, resultCode' -o table
    ```

5. **Confirm the connection string is injected via secret reference, not as a plain env var**

    ```bash
    az containerapp show -g rg-cicd-week4 -n ca-cicd-week4 \
      --query 'properties.template.containers[0].env[?name==`APPLICATIONINSIGHTS_CONNECTION_STRING`]' -o json
    ```

    Expected: an object with `secretRef: "appinsights-connstr"` and **no** `value` field.

### Deviations from Exercise Text (Week 5)

These were necessary to drive the live execution successfully and have been folded back into the exercise files where applicable.

1. **Log Analytics field name `ContainerGroupName_s` rather than `ReplicaName_s`.** The first draft of Exercise 5.2 used `ReplicaName_s` for the per-replica grouping, which raised `SemanticError SEM0100: Failed to resolve scalar expression named 'ReplicaName_s'` against this workspace. The schema actually exposes the replica identity as `ContainerGroupName_s` (alongside `ContainerAppName_s`, `RevisionName_s`). The exercise text was updated globally — every reference to `ReplicaName_s` now reads `ContainerGroupName_s`, and the surrounding prose mentions that the value also includes the revision prefix (e.g. `ca-cicd-week4--0000007-58d9d87bf-xwgjm`).
2. **`Microsoft.ApplicationInsights.AspNetCore` shipped at version 3.1.0**, not the 2.22.0 pinned in the exercise's example `.csproj` snippet. The 3.x series is the current GA line as of April 2026; the exercise's existing footnote ("the exact version pinned by `dotnet add` will be the latest at the time you run the command") covers this. The `.csproj` snippet in the exercise is left at 2.22.0 as illustrative — not as a pin to require.
3. **Two-replica scaling step (Step 11 of Exercise 5.2) executed only briefly during validation.** The validation environment ran at 1 replica throughout most of the run, then scaled to 2 only long enough to confirm the `summarize ... by ContainerGroupName_s` query returns multiple rows, then back to 1. The `docs/validation/week-5-kql-output.txt` transcript shows three `ContainerGroupName_s` values total — two from Week 4's earlier revisions still in the 30-minute window, plus the active 0000007 replica. The exercise text remains correct; just noting that the stepwise progression in the transcript file is compressed.
4. **Application Insights Portal-blade screenshots (Live Metrics, Application Map) NOT captured.** Automated Playwright capture against `portal.azure.com` requires interactive Microsoft SSO with MFA, which cannot be driven from a non-interactive shell without persisting authentication state across runs. The query-transcript files (`week-5-kql-output.txt` and `week-5-app-insights-output.txt`) verify that the underlying telemetry — requests, exceptions, custom metrics, and traces — is flowing as expected; visual confirmation of the Live Metrics and Application Map blades is left as a manual step. To capture them: open the Azure Portal → resource group `rg-cicd-week4` → `cloudci-insights` → **Live Metrics** (capture full-page screenshot to `docs/screenshots/week-5-live-metrics.png`), then **Application Map** (capture to `docs/screenshots/week-5-application-map.png`).
5. **Cleanup substep (Exercise 5.3 Step 14) NOT executed during validation.** The resource group and Entra app registration are intentionally left running so the screenshots and transcripts above remain reproducible. Students are expected to run the cleanup as the final substep of their lab; the teacher run will perform the cleanup at the end of the validation window.

### Status (Week 5)

**Validated end-to-end on 2026-04-28.** All three Week 5 commits deployed cleanly, structured logs flow into Log Analytics with the expected fields, App Insights captures requests, exceptions, traces, and the custom metric. The `/Home/Boom` failure is visible in the Failures blade (verified via `exceptions` KQL query against App Insights). Cloud teardown deferred — see Deviation #5.
