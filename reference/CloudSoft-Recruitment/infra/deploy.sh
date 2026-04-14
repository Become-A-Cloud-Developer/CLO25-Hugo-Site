#!/usr/bin/env bash
# ════════════════════════════════════════════════════════════════
# CloudSoft Recruitment Portal — Full Deployment Script
# ════════════════════════════════════════════════════════════════
#
# Usage: ./deploy.sh [environment]
#   environment: dev (default), test, prod
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV="${1:-dev}"
RESOURCE_GROUP="cloudsoft-${ENV}-rg"
LOCATION="norwayeast"
IMAGE_NAME="recruitment-api"
IMAGE_TAG="${2:-latest}"

echo "╔══════════════════════════════════════════════════╗"
echo "║  CloudSoft Recruitment Portal — Deployment       ║"
echo "║  Environment: ${ENV}                              "
echo "╚══════════════════════════════════════════════════╝"
echo ""

# ── Pre-flight: Generate secrets for Bicep deployment ───────────
export ADMIN_SEED_PASSWORD="${ADMIN_SEED_PASSWORD:-Admin123!}"
export CANDIDATE_SEED_PASSWORD="${CANDIDATE_SEED_PASSWORD:-Candidate123!}"
export JWT_KEY="${JWT_KEY:-$(openssl rand -base64 32)}"
echo "▶ Pre-flight: Secrets generated for deployment"
echo ""

# ── Step 1: Create Resource Group ───────────────────────────────
echo "▶ Step 1: Creating resource group '${RESOURCE_GROUP}' in '${LOCATION}'..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output none
echo "  ✓ Resource group ready"
echo ""

# ── Step 2: Deploy Bicep Infrastructure ─────────────────────────
echo "▶ Step 2: Deploying Bicep infrastructure..."

# Use a placeholder image for initial deployment
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file "${SCRIPT_DIR}/bicep/main.bicep" \
  --parameters "${SCRIPT_DIR}/bicep/parameters/${ENV}.bicepparam" \
  --query 'properties.outputs' \
  --output json)

ACR_LOGIN_SERVER=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.acrLoginServer.value')
KEY_VAULT_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.keyVaultName.value')
CONTAINER_APP_NAME=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.containerAppName.value')
APP_URL=$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.appUrl.value')

echo "  ✓ Infrastructure deployed"
echo "    ACR:           ${ACR_LOGIN_SERVER}"
echo "    Key Vault:     ${KEY_VAULT_NAME}"
echo "    Container App: ${CONTAINER_APP_NAME}"
echo ""

# ── Step 3: Build and Push Container Image ──────────────────────
echo "▶ Step 3: Building and pushing container image..."

# Log in to ACR
az acr login --name "$ACR_LOGIN_SERVER" --output none

# Build and push using ACR Tasks (no local Docker needed)
az acr build \
  --registry "$ACR_LOGIN_SERVER" \
  --image "${IMAGE_NAME}:${IMAGE_TAG}" \
  --file "${SCRIPT_DIR}/../src/CloudSoft.Web/Dockerfile" \
  --build-arg VERSION=1.0.0+${IMAGE_TAG} \
  "${SCRIPT_DIR}/../src/CloudSoft.Web/" \
  --output none

FULL_IMAGE="${ACR_LOGIN_SERVER}/${IMAGE_NAME}:${IMAGE_TAG}"
echo "  ✓ Image pushed: ${FULL_IMAGE}"
echo ""

# ── Step 4: Update Container App with Real Image ────────────────
echo "▶ Step 4: Updating container app with built image..."
az containerapp update \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --image "$FULL_IMAGE" \
  --output none
echo "  ✓ Container app updated"
echo ""

# ── Step 5: Populate Key Vault Secrets ─────────────────────────
echo "▶ Step 5: Populating Key Vault secrets..."

# Grant deploying user Key Vault Secrets Officer role
USER_OID=$(az ad signed-in-user show --query id -o tsv 2>/dev/null || echo "")
if [ -n "$USER_OID" ]; then
  KEY_VAULT_ID=$(az keyvault show --name "$KEY_VAULT_NAME" --resource-group "$RESOURCE_GROUP" --query id -o tsv)
  az role assignment create \
    --role "Key Vault Secrets Officer" \
    --assignee "$USER_OID" \
    --scope "$KEY_VAULT_ID" \
    --output none 2>/dev/null && echo "  ✓ Key Vault RBAC granted" || echo "  ⚠ RBAC already assigned or failed"
  echo "  Waiting for RBAC propagation..."
  sleep 15
fi

# Get storage and cosmos connection strings from Azure
STORAGE_CONN=$(az storage account show-connection-string \
  --name "$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.storageAccountName.value')" \
  --resource-group "$RESOURCE_GROUP" \
  --query connectionString --output tsv 2>/dev/null || echo "")

COSMOS_CONN=$(az cosmosdb keys list \
  --type connection-strings \
  --name "$(echo "$DEPLOYMENT_OUTPUT" | jq -r '.cosmosDbName.value')" \
  --resource-group "$RESOURCE_GROUP" \
  --query "connectionStrings[0].connectionString" --output tsv 2>/dev/null || echo "")

az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "AdminSeed--Password" --value "$ADMIN_SEED_PASSWORD" --output none 2>/dev/null && echo "  ✓ AdminSeed--Password set" || echo "  ⚠ Failed to set AdminSeed--Password (may need RBAC)"

az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "Jwt--Key" --value "$JWT_KEY" --output none 2>/dev/null && echo "  ✓ Jwt--Key set" || echo "  ⚠ Failed to set Jwt--Key"

az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "Jwt--Issuer" --value "https://${APP_URL}" --output none 2>/dev/null && echo "  ✓ Jwt--Issuer set" || echo "  ⚠ Failed to set Jwt--Issuer"

az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "Jwt--Audience" --value "cloudsoft-recruitment" --output none 2>/dev/null && echo "  ✓ Jwt--Audience set" || echo "  ⚠ Failed to set Jwt--Audience"

if [ -n "$COSMOS_CONN" ]; then
  az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "MongoDb--ConnectionString" --value "$COSMOS_CONN" --output none 2>/dev/null && echo "  ✓ MongoDb--ConnectionString set"
fi

if [ -n "$STORAGE_CONN" ]; then
  az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "BlobStorage--ConnectionString" --value "$STORAGE_CONN" --output none 2>/dev/null && echo "  ✓ BlobStorage--ConnectionString set"
fi

echo ""
echo "📋 Optional: Set Google OAuth secrets manually:"
echo "  az keyvault secret set --vault-name $KEY_VAULT_NAME --name Google--ClientId --value <client-id>"
echo "  az keyvault secret set --vault-name $KEY_VAULT_NAME --name Google--ClientSecret --value <secret>"

# ── Done ────────────────────────────────────────────────────────
echo "╔══════════════════════════════════════════════════╗"
echo "║  Deployment Complete!                            ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""
echo "  Application URL: ${APP_URL}"
echo ""
echo "  Next steps:"
echo "    1. Add secrets to Key Vault (see commands above)"
echo "    2. Verify the application: curl ${APP_URL}/health"
echo ""
