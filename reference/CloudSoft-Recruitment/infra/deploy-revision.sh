#!/usr/bin/env bash
# ════════════════════════════════════════════════════════════════
# CloudSoft Recruitment Portal — Revision & Traffic Splitting
# ════════════════════════════════════════════════════════════════
#
# Deploys a new revision and sets up traffic splitting.
#
# Usage: ./deploy-revision.sh [environment] [image-tag] [new-traffic-pct]
#   environment:      dev (default), test, prod
#   image-tag:        Tag for the new image (default: latest)
#   new-traffic-pct:  Percentage of traffic to the new revision (default: 20)
#
# Examples:
#   ./deploy-revision.sh dev v2 20       # 80% old / 20% new
#   ./deploy-revision.sh dev v2 100      # Full cutover to new revision
#   ./deploy-revision.sh dev v2 0        # Deploy but send no traffic (canary)
#
set -euo pipefail

ENV="${1:-dev}"
IMAGE_TAG="${2:-latest}"
NEW_TRAFFIC_PCT="${3:-20}"
OLD_TRAFFIC_PCT=$((100 - NEW_TRAFFIC_PCT))

RESOURCE_GROUP="cloudsoft-${ENV}-rg"
CONTAINER_APP_NAME="cloudsoft-${ENV}-app"
IMAGE_NAME="recruitment-api"

echo "╔══════════════════════════════════════════════════╗"
echo "║  CloudSoft — Revision Deployment                 ║"
echo "║  Environment: ${ENV}                              "
echo "║  Image tag:   ${IMAGE_TAG}                        "
echo "║  Traffic:     ${OLD_TRAFFIC_PCT}% old / ${NEW_TRAFFIC_PCT}% new  "
echo "╚══════════════════════════════════════════════════╝"
echo ""

# ── Step 1: Get current state ───────────────────────────────────
echo "▶ Step 1: Getting current container app state..."
ACR_LOGIN_SERVER=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query 'properties.configuration.registries[0].server' \
  --output tsv)

CURRENT_REVISION=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query 'properties.latestRevisionName' \
  --output tsv)

FULL_IMAGE="${ACR_LOGIN_SERVER}/${IMAGE_NAME}:${IMAGE_TAG}"

echo "  Current revision: ${CURRENT_REVISION}"
echo "  New image:        ${FULL_IMAGE}"
echo ""

# ── Step 2: Build and push new image ───────────────────────────
echo "▶ Step 2: Building and pushing new image '${IMAGE_TAG}'..."
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

az acr build \
  --registry "$ACR_LOGIN_SERVER" \
  --image "${IMAGE_NAME}:${IMAGE_TAG}" \
  --file "${SCRIPT_DIR}/../src/CloudSoft.Web/Dockerfile" \
  --build-arg VERSION=1.0.0+${IMAGE_TAG} \
  "${SCRIPT_DIR}/../src/CloudSoft.Web/" \
  --output none
echo "  ✓ Image pushed: ${FULL_IMAGE}"
echo ""

# ── Step 3: Create new revision ─────────────────────────────────
echo "▶ Step 3: Creating new revision..."

# Use a revision suffix based on the image tag (sanitised for ACA naming rules)
REVISION_SUFFIX=$(echo "$IMAGE_TAG" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9-]/-/g' | head -c 10)

az containerapp update \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --image "$FULL_IMAGE" \
  --revision-suffix "$REVISION_SUFFIX" \
  --output none

NEW_REVISION=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query 'properties.latestRevisionName' \
  --output tsv)

echo "  ✓ New revision created: ${NEW_REVISION}"
echo ""

# ── Step 4: Configure traffic splitting ─────────────────────────
echo "▶ Step 4: Configuring traffic split (${OLD_TRAFFIC_PCT}/${NEW_TRAFFIC_PCT})..."

if [ "$NEW_TRAFFIC_PCT" -eq 100 ]; then
  # Full cutover — send all traffic to the new revision
  az containerapp ingress traffic set \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --revision-weight "${NEW_REVISION}=100" \
    --output none
else
  # Split traffic between old and new revisions
  az containerapp ingress traffic set \
    --name "$CONTAINER_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --revision-weight "${CURRENT_REVISION}=${OLD_TRAFFIC_PCT}" "${NEW_REVISION}=${NEW_TRAFFIC_PCT}" \
    --output none
fi

echo "  ✓ Traffic split configured"
echo ""

# ── Step 5: Show current traffic distribution ───────────────────
echo "▶ Step 5: Current traffic distribution:"
az containerapp ingress traffic show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --output table
echo ""

# ── Step 6: List all active revisions ──────────────────────────
echo "▶ Step 6: Active revisions:"
az containerapp revision list \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "[].{Name:name, Active:properties.active, TrafficWeight:properties.trafficWeight, Created:properties.createdTime}" \
  --output table
echo ""

# ── Done ────────────────────────────────────────────────────────
APP_FQDN=$(az containerapp show \
  --name "$CONTAINER_APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query 'properties.configuration.ingress.fqdn' \
  --output tsv)

echo "╔══════════════════════════════════════════════════╗"
echo "║  Revision Deployment Complete!                   ║"
echo "╚══════════════════════════════════════════════════╝"
echo ""
echo "  Application URL: https://${APP_FQDN}"
echo ""
echo "  To promote the new revision to 100%:"
echo "    ./deploy-revision.sh ${ENV} ${IMAGE_TAG} 100"
echo ""
echo "  To rollback (send all traffic to previous revision):"
echo "    az containerapp ingress traffic set \\"
echo "      --name ${CONTAINER_APP_NAME} \\"
echo "      --resource-group ${RESOURCE_GROUP} \\"
echo "      --revision-weight ${CURRENT_REVISION}=100"
echo ""
