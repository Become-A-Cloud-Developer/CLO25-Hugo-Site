// ════════════════════════════════════════════════════════════════
// CloudSoft Recruitment Portal — Infrastructure Orchestrator
// ════════════════════════════════════════════════════════════════
//
// Deploys all Azure resources for the CloudSoft Recruitment Portal:
//   ACR, Managed Identity, Key Vault, Cosmos DB (MongoDB),
//   Log Analytics, ACA Environment, Container App, Storage Account
//

targetScope = 'resourceGroup'

// ── Parameters ─────────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
@allowed(['dev', 'test', 'prod'])
param environment string

@description('Azure region — defaults to the resource group location')
param location string = resourceGroup().location

@description('Container image to deploy (e.g. myacr.azurecr.io/recruitment-api:latest)')
param containerImage string

@description('Admin seed password')
@secure()
param adminSeedPassword string

@description('Candidate seed password')
@secure()
param candidateSeedPassword string

@description('JWT signing key (at least 32 characters)')
@secure()
param jwtKey string

// ── Modules ────────────────────────────────────────────────────

module acr 'modules/container-registry.bicep' = {
  name: 'container-registry'
  params: {
    environment: environment
    location: location
    managedIdentityPrincipalId: identity.outputs.principalId
  }
}

module identity 'modules/managed-identity.bicep' = {
  name: 'managed-identity'
  params: {
    environment: environment
    location: location
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    environment: environment
    location: location
    managedIdentityPrincipalId: identity.outputs.principalId
  }
}

module cosmosDb 'modules/cosmos-db.bicep' = {
  name: 'cosmos-db'
  params: {
    environment: environment
    location: location
  }
}

module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'log-analytics'
  params: {
    environment: environment
    location: location
  }
}

module appInsights 'modules/application-insights.bicep' = {
  name: 'application-insights'
  params: {
    environment: environment
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.workspaceId
  }
}

module containerAppsEnv 'modules/container-apps-env.bicep' = {
  name: 'container-apps-env'
  params: {
    environment: environment
    location: location
    logAnalyticsCustomerId: logAnalytics.outputs.customerId
    logAnalyticsSharedKey: logAnalytics.outputs.sharedKey
  }
}

module storageAccount 'modules/storage-account.bicep' = {
  name: 'storage-account'
  params: {
    environment: environment
    location: location
  }
}

module containerApp 'modules/container-app.bicep' = {
  name: 'container-app'
  params: {
    environment: environment
    location: location
    containerAppsEnvId: containerAppsEnv.outputs.id
    containerImage: containerImage
    acrLoginServer: acr.outputs.loginServer
    managedIdentityId: identity.outputs.id
    managedIdentityClientId: identity.outputs.clientId
    keyVaultUri: keyVault.outputs.vaultUri
    applicationInsightsConnectionString: appInsights.outputs.connectionString
    adminSeedPassword: adminSeedPassword
    candidateSeedPassword: candidateSeedPassword
    jwtKey: jwtKey
  }
}

// ── Key Vault Secrets ──────────────────────────────────────────
// Connection strings stored in Key Vault, overriding env var placeholders.
// Secret names use '--' separator (Azure Key Vault convention for ':' in .NET config).

module kvSecrets 'modules/key-vault-secrets.bicep' = {
  name: 'key-vault-secrets'
  params: {
    keyVaultName: keyVault.outputs.name
    cosmosConnectionString: cosmosDb.outputs.connectionString
    blobConnectionString: storageAccount.outputs.connectionString
  }
}

// ── Outputs ────────────────────────────────────────────────────

@description('Container App FQDN — the public URL of the application')
output appFqdn string = containerApp.outputs.fqdn

@description('Container App URL')
output appUrl string = 'https://${containerApp.outputs.fqdn}'

@description('ACR login server')
output acrLoginServer string = acr.outputs.loginServer

@description('Key Vault name (for adding secrets)')
output keyVaultName string = keyVault.outputs.name

@description('Cosmos DB account name')
output cosmosDbName string = cosmosDb.outputs.name

@description('Storage Account name')
output storageAccountName string = storageAccount.outputs.name

@description('Container App name')
output containerAppName string = containerApp.outputs.name

@description('Managed Identity client ID')
output managedIdentityClientId string = identity.outputs.clientId

@description('Application Insights name')
output applicationInsightsName string = appInsights.outputs.name
