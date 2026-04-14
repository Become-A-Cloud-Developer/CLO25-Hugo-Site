// ──────────────────────────────────────────────
// Azure Container Registry — Basic tier
// ──────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
param environment string

@description('Azure region for the resource')
param location string

@description('Principal ID of the managed identity to grant AcrPull')
param managedIdentityPrincipalId string

var uniqueSuffix = uniqueString(resourceGroup().id)
var acrName = replace('cloudsoft-${environment}-acr-${uniqueSuffix}', '-', '')

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// AcrPull role assignment for the managed identity
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, managedIdentityPrincipalId, '7f951dda-4ed3-4680-a7ca-43fe172d538d')
  scope: acr
  properties: {
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d' // AcrPull
    )
  }
}

@description('The ACR login server (e.g. myacr.azurecr.io)')
output loginServer string = acr.properties.loginServer

@description('The ACR resource name')
output name string = acr.name

@description('The ACR resource ID')
output id string = acr.id
