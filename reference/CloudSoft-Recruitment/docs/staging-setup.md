# Staging Environment Setup

This documents how to set up and deploy the staging environment on Azure. The staging environment runs the application with in-memory data (no CosmosDB or Blob Storage) — it validates the CI/CD pipeline, container image, and ACA deployment.

## Prerequisites

- Azure CLI (`az`) logged in
- GitHub CLI (`gh`) authenticated
- Git repository pushed to GitHub

## Step 1: Create the Azure Resource Group

```bash
az group create --name cloudsoft-stage-rg --location norwayeast
```

## Step 2: Deploy Infrastructure (ACR + ACA)

The staging Bicep template provisions only what's needed:
- Azure Container Registry (Basic)
- Log Analytics workspace (required by ACA)
- Container Apps Environment
- Container App (with placeholder image initially)

```bash
az deployment group create \
  --resource-group cloudsoft-stage-rg \
  --template-file infra/bicep/staging.bicep \
  --query 'properties.outputs' -o json
```

Note the outputs — you'll need `acrName` and `acrLoginServer` for the GitHub secrets.

## Step 3: Create a Service Principal

Create a service principal with Contributor access to the resource group and AcrPush access to the registry:

```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)

# Contributor on the resource group
az ad sp create-for-rbac \
  --name "cloudsoft-github-actions" \
  --role Contributor \
  --scopes "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/cloudsoft-stage-rg" \
  --json-auth

# Save the JSON output — it contains clientId, clientSecret, subscriptionId, tenantId

# AcrPush on the container registry
ACR_ID=$(az acr show --name <acr-name> --resource-group cloudsoft-stage-rg --query id -o tsv)

az role assignment create \
  --assignee <clientId-from-above> \
  --role AcrPush \
  --scope "$ACR_ID"
```

## Step 4: Set GitHub Secrets

```bash
# Azure credentials (paste the full JSON from Step 3)
gh secret set AZURE_CREDENTIALS < credentials.json
# Or: echo '{"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}' | gh secret set AZURE_CREDENTIALS

# ACR details
gh secret set ACR_LOGIN_SERVER --body "<acr-login-server>.azurecr.io"
gh secret set ACR_NAME --body "<acr-name>"
```

Verify secrets are set:
```bash
gh secret list
```

## Step 5: Push Code and Trigger the Pipeline

```bash
git push

# Trigger the staging workflow manually
gh workflow run staging.yml --ref main

# Watch the run
gh run list --workflow=staging.yml --limit 1
```

The pipeline runs three steps:
1. **build-and-test** — restores, builds, runs unit/integration tests on ubuntu
2. **build-push-deploy** — builds Docker image (amd64), pushes to ACR, deploys to ACA
3. **verify** — checks health, version, API, and homepage endpoints

## Step 6: Verify the Deployment

### From the terminal

```bash
APP_URL="https://<app-fqdn>"

# Health check
curl -sf "$APP_URL/health"

# Version (should show git commit hash)
curl -sf "$APP_URL/version"

# API endpoint
curl -sf "$APP_URL/api/jobs"

# JWT token flow
TOKEN=$(curl -sf -X POST "$APP_URL/api/token" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@cloudsoft.com","password":"Admin123!"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# Create a job via API
curl -sf -X POST "$APP_URL/api/jobs" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"title":"Test Job","description":"Testing staging","location":"Oslo","deadline":"2026-05-01"}'

# List jobs
curl -sf "$APP_URL/api/jobs"
```

### Run smoke tests

```bash
SMOKE_TEST_BASE_URL="https://<app-fqdn>" \
  dotnet test tests/CloudSoft.Web.Tests/ --filter "FullyQualifiedName~SmokeTests"
```

### Browse the application

```bash
open "https://<app-fqdn>"
```

Login with:
- **Admin:** admin@cloudsoft.com / Admin123!
- **Candidate:** candidate@test.com / Candidate123!

## What's Running in Staging

| Component | Configuration |
|---|---|
| Data | In-memory repositories (no database) |
| Blobs | Local disk inside container |
| Identity | SQLite at /app/data/identity.db |
| Google OAuth | Disabled |
| Key Vault | Disabled |
| Application Insights | Disabled |
| REST Countries | Enabled |
| JWT | Enabled (staging signing key) |

Data does not persist across container restarts (in-memory). Identity data persists within a single container lifecycle but is lost on redeployment (no volume mount in staging).

## Staging Pipeline Workflow

The workflow is defined in `.github/workflows/staging.yml` and triggered manually:

```bash
gh workflow run staging.yml --ref main
```

It uses `workflow_dispatch` only (no automatic trigger on push).

## Teardown

To remove all staging resources:

```bash
az group delete --name cloudsoft-stage-rg --yes --no-wait
```

To remove the service principal:

```bash
az ad sp delete --id <clientId>
```

## Troubleshooting

### Container crashes (exit code 139)

Check if `ConnectionStrings__Identity` is set in the Bicep env vars. The default `Data Source=identity.db` writes to the non-writable working directory. It must be `Data Source=/app/data/identity.db`.

### Health probe fails with 404

The placeholder image (`mcr.microsoft.com/dotnet/samples:aspnetapp`) doesn't have a `/health` endpoint. Deploy the real image via the CI/CD pipeline.

### Image architecture mismatch

Never build Docker images locally on a Mac — ARM images won't run on ACA (amd64). Always build through the GitHub Actions pipeline which runs on ubuntu.

### Check container logs

```bash
# Application logs
az containerapp logs show --name cloudsoft-stage-app --resource-group cloudsoft-stage-rg --type console --tail 30

# System/platform logs
az containerapp logs show --name cloudsoft-stage-app --resource-group cloudsoft-stage-rg --type system --tail 30
```
