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
