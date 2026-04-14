// ──────────────────────────────────────────────
// Key Vault Secrets — connection strings + feature flags
// ──────────────────────────────────────────────
// Stores connection strings and feature flags in Key Vault.
// The app's Azure Key Vault config provider (highest precedence)
// overrides container app env var defaults.
//
// NOTE: FeatureFlags:UseKeyVault is NOT stored here — it's the
// bootstrap flag that decides whether to load Key Vault at all.

@description('Name of the Key Vault')
param keyVaultName string

@description('Cosmos DB (MongoDB) connection string')
@secure()
param cosmosConnectionString string

@description('Blob Storage connection string')
@secure()
param blobConnectionString string

// Feature flags — toggle in Key Vault, restart container to apply.
@description('Enable MongoDB (true) or in-memory (false)')
param useMongoDB string = 'true'

@description('Enable Azure Blob Storage (true) or local disk (false)')
param useBlobStorage string = 'true'

@description('Enable Google OAuth login')
param useGoogleAuth string = 'false'

@description('Enable Application Insights telemetry')
param useApplicationInsights string = 'true'

@description('Enable REST Countries API')
param useRestCountries string = 'true'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// ── Connection strings ────────────────────────

resource cosmosSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'MongoDb--ConnectionString'
  properties: {
    value: cosmosConnectionString
  }
}

resource blobSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'BlobStorage--ConnectionString'
  properties: {
    value: blobConnectionString
  }
}

// ── Feature flags ─────────────────────────────

resource flagMongoDB 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'FeatureFlags--UseMongoDB'
  properties: {
    value: useMongoDB
  }
}

resource flagBlobStorage 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'FeatureFlags--UseBlobStorage'
  properties: {
    value: useBlobStorage
  }
}

resource flagGoogleAuth 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'FeatureFlags--UseGoogleAuth'
  properties: {
    value: useGoogleAuth
  }
}

resource flagApplicationInsights 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'FeatureFlags--UseApplicationInsights'
  properties: {
    value: useApplicationInsights
  }
}

resource flagRestCountries 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'FeatureFlags--UseRestCountries'
  properties: {
    value: useRestCountries
  }
}
