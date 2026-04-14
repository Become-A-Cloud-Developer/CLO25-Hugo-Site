# Production Setup — CloudSoft Recruitment Portal

Deployed 2026-04-07. All infrastructure is defined in Bicep and deployed via CI/CD.

## Azure Resources

| Resource | Name | Purpose |
|----------|------|---------|
| Resource Group | `cloudsoft-prod-rg` | All production resources |
| Container Registry | `cloudsoftprodacr3tfb22c4kec6k` | Docker images |
| Container App | `cloudsoft-prod-app` | The web application |
| Container Apps Env | `cloudsoft-prod-aca-env` | ACA hosting environment |
| Cosmos DB (MongoDB) | `cloudsoft-prod-cosmos-*` | Job and application data |
| Key Vault | `cs-prod-kv-*` | Secrets management |
| Storage Account | `csprodst*` | Blob storage (CVs) |
| App Insights | `cloudsoft-prod-ai` | Telemetry |
| Log Analytics | `cloudsoft-prod-logs` | Container logs |
| Managed Identity | `cloudsoft-prod-identity` | RBAC for Key Vault + ACR |

## App URL

```
https://cloudsoft-prod-app.thankfulplant-484cfdb4.norwayeast.azurecontainerapps.io
```

## CI/CD Pipeline

**Workflow:** `.github/workflows/production.yml` (manual trigger)

**Authentication:** OIDC federation (no client secrets)
- App Registration: `cloudsoft-prod-github-actions`
- Federated credential: `repo:larsappel/CLO25-Advanced:ref:refs/heads/main`

**GitHub Secrets:**
- `AZURE_PROD_CLIENT_ID` — OIDC app registration client ID
- `AZURE_PROD_TENANT_ID` — Azure AD tenant
- `AZURE_PROD_SUBSCRIPTION_ID` — Azure subscription
- `PROD_ACR_NAME` — ACR resource name
- `PROD_ACR_LOGIN_SERVER` — ACR login server FQDN

**Pipeline steps:**
1. Build and test (.NET)
2. Docker build on ubuntu (amd64), push to ACR
3. `az containerapp update` with new image
4. Health check verification with retries

## One-Time Setup (not in Bicep)

These resources are outside the Bicep template scope and were created manually. They only need to be done once per environment.

### 1. Resource Group

```bash
az group create --name cloudsoft-prod-rg --location norwayeast
```

### 2. OIDC Federation (GitHub → Azure)

Creates a passwordless trust between GitHub Actions and Azure — no client secrets to rotate.

```bash
# Create app registration
az ad app create --display-name "cloudsoft-prod-github-actions"
APP_ID=$(az ad app list --display-name "cloudsoft-prod-github-actions" --query "[0].appId" -o tsv)

# Create service principal
az ad sp create --id "$APP_ID"

# Add federated credential for GitHub Actions on main branch
APP_OBJECT_ID=$(az ad app list --display-name "cloudsoft-prod-github-actions" --query "[0].id" -o tsv)
az ad app federated-credential create --id "$APP_OBJECT_ID" --parameters '{
  "name": "github-actions-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:larsappel/CLO25-Advanced:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
```

### 3. Role Assignments for OIDC Service Principal

```bash
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
RG_SCOPE="/subscriptions/$SUBSCRIPTION_ID/resourceGroups/cloudsoft-prod-rg"

# Contributor on resource group (for az containerapp update)
az role assignment create --assignee "$APP_ID" --role Contributor --scope "$RG_SCOPE"

# AcrPush on ACR (for docker push) — run after Bicep creates the ACR
ACR_ID=$(az acr show --name <acr-name> --resource-group cloudsoft-prod-rg --query id -o tsv)
az role assignment create --assignee "$APP_ID" --role AcrPush --scope "$ACR_ID"
```

### 4. GitHub Secrets

```bash
gh secret set AZURE_PROD_CLIENT_ID --body "$APP_ID"
gh secret set AZURE_PROD_TENANT_ID --body "$(az account show --query tenantId -o tsv)"
gh secret set AZURE_PROD_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID"
gh secret set PROD_ACR_LOGIN_SERVER --body "<acr>.azurecr.io"
gh secret set PROD_ACR_NAME --body "<acr-name>"
```

### 5. Key Vault Access for Operators

Grant yourself access to manage secrets manually (e.g. rotating keys):

```bash
USER_OID=$(az ad signed-in-user show --query id -o tsv)
KV_ID=$(az keyvault show --name <kv-name> --resource-group cloudsoft-prod-rg --query id -o tsv)
az role assignment create --role "Key Vault Secrets Officer" --assignee "$USER_OID" --scope "$KV_ID"
```

## Deploy Infrastructure

```bash
# Rebuild and deploy Bicep (idempotent)
az bicep build --file infra/bicep/main.bicep --outfile /tmp/main.json

# Use REST API (az deployment group create has a streaming bug in CLI 2.76.0)
python3 -c "
import json
with open('/tmp/main.json') as f:
    template = json.load(f)
body = {'properties': {'mode': 'Incremental', 'template': template, 'parameters': {
    'environment': {'value': 'prod'}, 'location': {'value': 'norwayeast'},
    'containerImage': {'value': '<acr>.azurecr.io/cloudsoft-web:<sha>'},
    'adminSeedPassword': {'value': '<password>'},
    'candidateSeedPassword': {'value': '<password>'},
    'jwtKey': {'value': '<key>'}
}}}
with open('/tmp/deploy.json', 'w') as f: json.dump(body, f)
"

ACCESS_TOKEN=$(az account get-access-token --query accessToken -o tsv)
curl -X PUT "https://management.azure.com/subscriptions/<sub>/resourcegroups/cloudsoft-prod-rg/providers/Microsoft.Resources/deployments/<name>?api-version=2024-03-01" \
  -H "Authorization: Bearer $ACCESS_TOKEN" -H "Content-Type: application/json" -d @/tmp/deploy.json
```

## Deploy Application Code

```bash
gh workflow run production.yml --ref main
gh run list --workflow=production.yml --limit 1
```

## Verify

```bash
APP_URL=https://cloudsoft-prod-app.thankfulplant-484cfdb4.norwayeast.azurecontainerapps.io

curl -sf "$APP_URL/health"
curl -sf "$APP_URL/version"
curl -sf "$APP_URL/api/jobs"
```

## Feature Flag Toggling (via Key Vault)

Feature flags are stored in Key Vault and override container app env vars. Change a flag and restart the container — no code deploy needed.

```bash
# Example: disable REST Countries API
az keyvault secret set --vault-name cs-prod-kv-3tfb22c4kec6k \
  --name "FeatureFlags--UseRestCountries" --value "false"

# Restart to pick up the change
REVISION=$(az containerapp revision list --name cloudsoft-prod-app \
  --resource-group cloudsoft-prod-rg --query "[?properties.active].name" -o tsv)
az containerapp revision restart --name cloudsoft-prod-app \
  --resource-group cloudsoft-prod-rg --revision "$REVISION"
```

| Flag | Key Vault Secret Name | Default |
|------|----------------------|---------|
| MongoDB | `FeatureFlags--UseMongoDB` | `true` |
| Blob Storage | `FeatureFlags--UseBlobStorage` | `true` |
| Google OAuth | `FeatureFlags--UseGoogleAuth` | `false` |
| Application Insights | `FeatureFlags--UseApplicationInsights` | `true` |
| REST Countries | `FeatureFlags--UseRestCountries` | `true` |
| Key Vault (bootstrap) | env var only — cannot be in KV | `true` |

## Key Decisions

1. **No Azure Files volume mount for SQLite** — SQLite is incompatible with SMB (mmap, fcntl locking, journal recovery all fail). Identity data is ephemeral but recreated on startup via `Migrate()` + seed code. See `docs/bugs/004-solution-remove-azurefiles-mount.md`.

2. **Single revision mode** — prevents concurrent containers from competing for resources.

3. **OIDC federation** — no client secrets stored in GitHub. Uses federated identity with Azure AD.

4. **Candidate seeding in all environments** — when `CandidateSeed:Password` is configured, the candidate user is seeded regardless of environment.

## Troubleshooting

```bash
# Container logs
az containerapp logs show --name cloudsoft-prod-app --resource-group cloudsoft-prod-rg --type console --tail 50

# System events
az containerapp logs show --name cloudsoft-prod-app --resource-group cloudsoft-prod-rg --type system --tail 30

# Log Analytics query
WORKSPACE_ID=$(az monitor log-analytics workspace show --workspace-name cloudsoft-prod-logs --resource-group cloudsoft-prod-rg --query customerId -o tsv)
az monitor log-analytics query --workspace "$WORKSPACE_ID" --analytics-query "ContainerAppConsoleLogs_CL | order by TimeGenerated desc | take 50"

# Revision status
az containerapp revision list --name cloudsoft-prod-app --resource-group cloudsoft-prod-rg -o table
```
