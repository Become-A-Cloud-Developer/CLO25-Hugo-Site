// ──────────────────────────────────────────────
// Azure Storage Account — CVs + Data Protection
// ──────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
param environment string

@description('Azure region for the resource')
param location string

var uniqueSuffix = uniqueString(resourceGroup().id)
var storageName = take(replace('cs${environment}st${uniqueSuffix}', '-', ''), 24)

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource cvsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'cvs'
  properties: {
    publicAccess: 'None'
  }
}

resource dataprotectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobServices
  name: 'dataprotection'
  properties: {
    publicAccess: 'None'
  }
}

resource fileServices 'Microsoft.Storage/storageAccounts/fileServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource identityFileShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-05-01' = {
  parent: fileServices
  name: 'identity-data'
  properties: {
    shareQuota: 1
    accessTier: 'TransactionOptimized'
  }
}

@description('The Storage Account name')
output name string = storageAccount.name

@description('The Storage Account resource ID')
output id string = storageAccount.id

@description('The Storage Account primary blob endpoint')
output primaryBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob

@description('The Storage Account access key')
output storageAccountKey string = storageAccount.listKeys().keys[0].value

@description('The Storage Account connection string')
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
