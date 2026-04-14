# Production Deployment Plan

This plan describes how to deploy the CloudSoft Recruitment Portal to Azure with all dependencies, verify it works end-to-end, and ensure all fixes are in the source code (not just patched at infrastructure level).

## Prerequisites

- Azure CLI (`az`) logged in with subscription access
- GitHub CLI (`gh`) authenticated with repo access
- Git working tree clean, all code pushed to `main`
- The staging environment (`cloudsoft-stage-rg`) is already deployed and working as reference

## Phase 0: Context Analysis

Before any implementation, read and understand the full project context.

### 0.1 Read Architecture Documents
- `docs/FEATURES.md` — all features and runtime scenarios
- `docs/ARCHITECTURE.md` — production diagram, data flow, runtime scenarios table
- `docs/IAM-ARCHITECTURE.md` — all authentication boundaries
- `docs/PRD.md` — full product requirements
- `docs/staging-setup.md` — what was done for staging (patterns to follow, bugs encountered)

### 0.2 Read Current Infrastructure Code
- `infra/bicep/main.bicep` — orchestrator, all module references and parameter wiring
- `infra/bicep/modules/*.bicep` — all 9 modules, understand parameters, outputs, and RBAC roles
- `infra/bicep/staging.bicep` — the minimal template that works (reference for what's proven)
- `.github/workflows/staging.yml` — the working CI/CD pipeline (reference)
- `.github/workflows/ci-cd.yml` — the Docker Hub pipeline (not used for production)

### 0.3 Read Application Configuration
- `src/CloudSoft.Web/Program.cs` — all feature flag checks, conditional service registration, Key Vault provider
- `src/CloudSoft.Web/appsettings.json` — base config with all keys
- `src/CloudSoft.Web/appsettings.Production.json` — production feature flags (all enabled)
- `src/CloudSoft.Web/Dockerfile` — VERSION build arg, data directory permissions
- `docker-compose.yml` — reference for which env vars the app needs

### 0.4 Read Test Infrastructure
- `tests/CloudSoft.Web.Tests/SmokeTests/DeploymentSmokeTests.cs` — what the smoke tests verify
- `tests/CloudSoft.Web.PlaywrightTests/` — E2E tests that could run against the production URL

### 0.5 Identify Known Issues from Staging
Review the staging deployment experience for issues likely to recur:
1. SQLite path must be `/app/data/identity.db` (not default `identity.db`)
2. Docker images must be built on amd64 (GitHub Actions ubuntu), never locally on Mac
3. Health probe on placeholder image returns 404 — expect initial startup failure
4. Azurite API version mismatch (not relevant for production, but shows SDK version sensitivity)
5. Google OAuth requires HTTPS and correct redirect URIs
6. Multiple `AddAuthentication()` calls caused Google scheme to not register

## Phase 1: Fix Staging Bicep (Hardcoded Secrets)

The staging Bicep currently hardcodes `Admin123!` and `Candidate123!`. Fix this before proceeding to production.

### 1.1 Update `infra/bicep/staging.bicep`
- Add `@secure() param adminSeedPassword string`
- Add `@secure() param candidateSeedPassword string`
- Add `@secure() param jwtKey string`
- Replace hardcoded values with parameter references
- Container app secrets reference the parameters

### 1.2 Redeploy Staging
```bash
az deployment group create \
  --resource-group cloudsoft-stage-rg \
  --template-file infra/bicep/staging.bicep \
  --parameters adminSeedPassword='Admin123!' candidateSeedPassword='Candidate123!' jwtKey='<generated>'
```

### 1.3 Verify Staging Still Works
```bash
SMOKE_TEST_BASE_URL=https://<staging-url> dotnet test tests/CloudSoft.Web.Tests/ --filter "FullyQualifiedName~SmokeTests"
```

## Phase 2: Prepare Production Bicep

### 2.1 Update `infra/bicep/main.bicep`
Add `@secure()` parameters for secrets that must be passed at deployment time:
- `adminSeedPassword`
- `candidateSeedPassword` (optional, for testing)
- `jwtKey`

These flow through to `container-app.bicep` as container app secrets.

### 2.2 Update `infra/bicep/modules/container-app.bicep`
- Add `@secure() param adminSeedPassword string`
- Add `@secure() param candidateSeedPassword string`
- Add `@secure() param jwtKey string`
- Add container app secrets for each
- Reference secrets in env vars via `secretRef`
- Verify all env vars match what Program.cs reads:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `FeatureFlags__UseMongoDB=true`
  - `FeatureFlags__UseBlobStorage=true`
  - `FeatureFlags__UseGoogleAuth=false` (enable later)
  - `FeatureFlags__UseKeyVault=true`
  - `FeatureFlags__UseApplicationInsights=true`
  - `FeatureFlags__UseRestCountries=true`
  - `MongoDb__ConnectionString` (secretRef from CosmosDB output)
  - `MongoDb__DatabaseName=cloudsoft-recruitment`
  - `ConnectionStrings__Identity=Data Source=/app/data/identity.db`
  - `BlobStorage__ConnectionString` (secretRef from Storage Account output)
  - `BlobStorage__ContainerName=cvs`
  - `AdminSeed__Password` (secretRef)
  - `CandidateSeed__Password` (secretRef)
  - `Jwt__Key` (secretRef)
  - `Jwt__Issuer=CloudSoft`
  - `Jwt__Audience=CloudSoftApi`
  - `KeyVault__VaultUri` (from Key Vault output)
  - `AZURE_CLIENT_ID` (from managed identity output)
  - `APPLICATIONINSIGHTS_CONNECTION_STRING` (from App Insights output)

### 2.3 Validate Bicep Locally
```bash
az bicep build --file infra/bicep/main.bicep
```

### 2.4 Handle Candidate Seeding in Production
Program.cs seeds candidates only in Development and Staging (`IsDevelopment() || IsStaging()`). For production testing, either:
- Pass `CandidateSeed__Password` and temporarily set `ASPNETCORE_ENVIRONMENT=Staging`, or
- Add a `SeedCandidate` feature flag, or
- Accept that production only has admin login (candidates register via Google OAuth)

Decision: Pass candidate password and add an environment check that also seeds when `CandidateSeed__Password` is configured, regardless of environment. This is the most flexible approach.

## Phase 3: Set Up OIDC Federation (GitHub ↔ Azure)

### 3.1 Create Azure AD App Registration
```bash
az ad app create --display-name "cloudsoft-prod-github-actions"
APP_ID=$(az ad app list --display-name "cloudsoft-prod-github-actions" --query "[0].appId" -o tsv)
```

### 3.2 Create Service Principal
```bash
az ad sp create --id "$APP_ID"
```

### 3.3 Assign Roles
```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RG_SCOPE="/subscriptions/$SUBSCRIPTION_ID/resourceGroups/cloudsoft-prod-rg"

# Contributor on resource group (for az containerapp update)
az role assignment create --assignee "$APP_ID" --role Contributor --scope "$RG_SCOPE"

# AcrPush on ACR (for docker push)
ACR_ID=$(az acr show --name <acr-name> --resource-group cloudsoft-prod-rg --query id -o tsv)
az role assignment create --assignee "$APP_ID" --role AcrPush --scope "$ACR_ID"
```

### 3.4 Add Federated Credential for GitHub
```bash
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-actions-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:larsappel/CLO25-Advanced:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

### 3.5 Set GitHub Secrets (No Client Secret Needed)
```bash
gh secret set AZURE_PROD_CLIENT_ID --body "$APP_ID"
gh secret set AZURE_PROD_TENANT_ID --body "$(az account show --query tenantId -o tsv)"
gh secret set AZURE_PROD_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID"
gh secret set PROD_ACR_LOGIN_SERVER --body "<acr>.azurecr.io"
gh secret set PROD_ACR_NAME --body "<acr-name>"
```

## Phase 4: Create Production CI/CD Workflow

### 4.1 Create `.github/workflows/production.yml`
- Trigger: `workflow_dispatch` only
- Jobs:
  1. **build-and-test**: restore, build, run unit/integration tests
  2. **build-push-deploy**: 
     - Azure login via OIDC (`azure/login@v2` with `client-id`, `tenant-id`, `subscription-id`)
     - ACR login
     - Docker build on ubuntu (amd64) with VERSION build arg
     - Push to ACR tagged with commit SHA
     - `az containerapp update` with new image
  3. **verify**:
     - Wait for container to start (check health endpoint with retries)
     - Verify health, version, API, homepage
     - Fail the workflow if health doesn't return 200

### 4.2 Key Difference from Staging Workflow
- Uses OIDC federation (no `AZURE_CREDENTIALS` secret, uses `client-id`/`tenant-id`/`subscription-id`)
- Points to production resource group and container app name
- Uses production ACR

## Phase 5: Deploy to Azure

### 5.1 Deploy Infrastructure
```bash
az deployment group create \
  --resource-group cloudsoft-prod-rg \
  --template-file infra/bicep/main.bicep \
  --parameters environment=prod location=norwayeast \
    containerImage=mcr.microsoft.com/dotnet/samples:aspnetapp \
    adminSeedPassword='Admin123!' \
    candidateSeedPassword='Candidate123!' \
    jwtKey="$(openssl rand -base64 32)"
```

Capture outputs: ACR name, Key Vault name, App URL, Container App name.

### 5.2 Grant Key Vault Access to Self (for manual secret management later)
```bash
USER_OID=$(az ad signed-in-user show --query id -o tsv)
az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee "$USER_OID" \
  --scope "/subscriptions/<sub>/resourceGroups/cloudsoft-prod-rg/providers/Microsoft.KeyVault/vaults/<vault-name>"
```

### 5.3 Push Code and Trigger Pipeline
```bash
git push
gh workflow run production.yml --ref main
```

### 5.4 Monitor Pipeline
```bash
gh run list --workflow=production.yml --limit 1
# Wait for completion, check logs on failure
```

## Phase 6: Verify Production

### 6.1 Smoke Tests
```bash
SMOKE_TEST_BASE_URL=https://<prod-app-url> \
  dotnet test tests/CloudSoft.Web.Tests/ --filter "FullyQualifiedName~SmokeTests"
```

### 6.2 API Flow Verification
```bash
APP_URL=https://<prod-app-url>

# Health
curl -sf "$APP_URL/health"

# Version (should show git commit hash)
curl -sf "$APP_URL/version"

# JWT token
TOKEN=$(curl -sf -X POST "$APP_URL/api/token" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@cloudsoft.com","password":"Admin123!"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# Create job (persists in CosmosDB)
curl -sf -X POST "$APP_URL/api/jobs" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Production Test","description":"Stored in CosmosDB","location":"Oslo","deadline":"2026-06-01"}'

# Verify job persists
curl -sf "$APP_URL/api/jobs"
```

### 6.3 Data Persistence Test
Verify data is in CosmosDB (not in-memory) by restarting the container and checking data survives:
```bash
# Create a job, note the title
# Restart the container
az containerapp revision restart --name <app> --resource-group cloudsoft-prod-rg --revision <revision>
# Wait for startup
sleep 30
# Verify job still exists
curl -sf "$APP_URL/api/jobs"
```

### 6.4 Blob Storage Test
Login as candidate via browser, apply for a job with a PDF CV, then verify:
- Upload succeeds
- Admin can download the CV
- File is in Azure Blob Storage (not local disk)

### 6.5 Application Insights Test
Check Azure Portal → Application Insights for:
- Request telemetry appearing
- Structured log entries
- No errors in the live metrics

### 6.6 Identity Persistence Test
Verify SQLite on Azure Files survives restarts:
```bash
# Login as admin (creates identity.db on Azure Files)
# Restart container
az containerapp revision restart ...
# Login again — should work (identity.db persisted)
```

### 6.7 Browse the Application
```bash
open "https://<prod-app-url>"
```
- Login as admin, create/edit/delete jobs
- Login as candidate, apply for a job
- Verify all pages render correctly

## Phase 7: Fix Issues and Iterate

For any issue found during verification:
1. **Diagnose** — check container logs: `az containerapp logs show --name <app> --resource-group cloudsoft-prod-rg --type console --tail 50`
2. **Fix in source code** — update Bicep, Program.cs, or workflow YAML
3. **Commit and push** — `git add && git commit && git push`
4. **Redeploy Bicep if infrastructure changed** — `az deployment group create ...` (idempotent)
5. **Trigger pipeline if code changed** — `gh workflow run production.yml`
6. **Re-verify** — run smoke tests again

Never fix issues only at the infrastructure level. All fixes must be in committed source code so the deployment is reproducible.

## Phase 8: Documentation and Report

### 8.1 Create `docs/production-setup.md`
Document the exact commands used, similar to `docs/staging-setup.md`.

### 8.2 Update `docs/IMPLEMENTATION-REPORT.md`
Add production deployment results.

### 8.3 Commit Everything
```bash
git add -A && git commit && git push
```

## Team Agent Strategy

### Agent 1: Infrastructure (Bicep + OIDC)
- Fix staging.bicep hardcoded secrets
- Update main.bicep and container-app.bicep with `@secure()` params
- Validate Bicep locally
- Set up OIDC federation (app registration, federated credential, roles)
- Set GitHub secrets
- Deploy infrastructure via `az deployment group create`

### Agent 2: CI/CD Pipeline
- Create `production.yml` workflow with OIDC login
- Ensure it builds on ubuntu (amd64), pushes to ACR, deploys to ACA
- Add verification step with health check retries

### Agent 3: Application Code (if needed)
- Fix candidate seeding to work when password is configured (any environment)
- Ensure Program.cs handles all production env vars correctly
- Any bug fixes found during deployment

### Agent 4: Verification
- Run smoke tests against production URL
- Test API flow (JWT, job CRUD, data persistence)
- Test blob storage (CV upload/download)
- Test identity persistence (restart container, verify login works)
- Check Application Insights in Azure Portal
- Browse the app manually
- Report results

### Agent 5: Review and Documentation
- Verify all fixes are in committed source code (not just patched in Azure)
- Compare running container's env vars against Bicep template
- Write production setup documentation
- Update implementation report

### Coordination
- Agents 1 and 2 work in parallel (infrastructure and pipeline are independent)
- Agent 3 runs if code changes are needed (based on Agent 1's findings)
- Agent 4 runs after deployment succeeds
- Agent 5 runs last, after everything is verified
- All agents commit their changes — never leave fixes uncommitted

## Key Principles

1. **All infrastructure is code** — Bicep is the source of truth, `az deployment group create` is idempotent
2. **No hardcoded secrets** — `@secure()` params passed at deployment time
3. **Build on CI/CD, not locally** — Mac ARM images don't run on Azure amd64
4. **Verify for real** — smoke tests against the live URL, not just mocked tests
5. **Fix in source, not in Azure** — every fix gets committed before redeploying
6. **OIDC over secrets** — no client secrets stored in GitHub, use federated identity
