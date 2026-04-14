// ──────────────────────────────────────────────
// Azure Container Apps Environment
// ──────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
param environment string

@description('Azure region for the resource')
param location string

@description('Log Analytics workspace customer ID')
param logAnalyticsCustomerId string

@description('Log Analytics workspace shared key')
@secure()
param logAnalyticsSharedKey string

var envName = 'cloudsoft-${environment}-aca-env'

resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
  }
}

@description('The Container Apps Environment resource ID')
output id string = containerAppsEnv.id

@description('The Container Apps Environment name')
output name string = containerAppsEnv.name

@description('The default domain of the Container Apps Environment')
output defaultDomain string = containerAppsEnv.properties.defaultDomain
