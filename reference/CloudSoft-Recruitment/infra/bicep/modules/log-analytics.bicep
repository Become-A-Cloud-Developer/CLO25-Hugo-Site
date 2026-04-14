// ──────────────────────────────────────────────
// Log Analytics Workspace (required by ACA)
// ──────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
param environment string

@description('Azure region for the resource')
param location string

var workspaceName = 'cloudsoft-${environment}-logs'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: workspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

@description('The Log Analytics workspace ID')
output workspaceId string = logAnalytics.id

@description('The Log Analytics customer ID (workspace GUID)')
output customerId string = logAnalytics.properties.customerId

@description('The Log Analytics shared key')
output sharedKey string = logAnalytics.listKeys().primarySharedKey

@description('The Log Analytics workspace name')
output name string = logAnalytics.name
