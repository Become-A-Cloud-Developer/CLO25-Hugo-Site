// ──────────────────────────────────────────────
// Azure Cosmos DB — MongoDB API 7.0, Serverless
// ──────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
param environment string

@description('Azure region for the resource')
param location string

var uniqueSuffix = uniqueString(resourceGroup().id)
var cosmosName = 'cloudsoft-${environment}-cosmos-${uniqueSuffix}'
var databaseName = 'cloudsoft-recruitment'

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' = {
  name: cosmosName
  location: location
  kind: 'MongoDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    capabilities: [
      { name: 'EnableMongo' }
      { name: 'EnableServerless' }
    ]
    apiProperties: {
      serverVersion: '7.0'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
  }
}

resource database 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases@2024-05-15' = {
  parent: cosmosAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource jobsCollection 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2024-05-15' = {
  parent: database
  name: 'jobs'
  properties: {
    resource: {
      id: 'jobs'
      shardKey: {
        _id: 'Hash'
      }
    }
  }
}

resource applicationsCollection 'Microsoft.DocumentDB/databaseAccounts/mongodbDatabases/collections@2024-05-15' = {
  parent: database
  name: 'applications'
  properties: {
    resource: {
      id: 'applications'
      shardKey: {
        _id: 'Hash'
      }
    }
  }
}

@description('The Cosmos DB connection string (primary)')
output connectionString string = cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString

@description('The Cosmos DB account name')
output name string = cosmosAccount.name

@description('The Cosmos DB resource ID')
output id string = cosmosAccount.id

@description('The database name')
output databaseName string = databaseName
