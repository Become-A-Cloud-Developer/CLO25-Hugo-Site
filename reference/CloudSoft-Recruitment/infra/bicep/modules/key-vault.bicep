// ──────────────────────────────────────────────
// Azure Key Vault + Secrets User role assignment
// ──────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
param environment string

@description('Azure region for the resource')
param location string

@description('Principal ID of the managed identity to grant Key Vault Secrets User')
param managedIdentityPrincipalId string

var uniqueSuffix = uniqueString(resourceGroup().id)
var kvName = take('cs-${environment}-kv-${uniqueSuffix}', 24)

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenant().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
  }
}

// ── Key Vault Secrets User role assignment ──
// Allows the managed identity to read secrets at runtime
resource kvSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentityPrincipalId, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: keyVault
  properties: {
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'
    )
  }
}

@description('The Key Vault URI')
output vaultUri string = keyVault.properties.vaultUri

@description('The Key Vault resource name')
output name string = keyVault.name

@description('The Key Vault resource ID')
output id string = keyVault.id
