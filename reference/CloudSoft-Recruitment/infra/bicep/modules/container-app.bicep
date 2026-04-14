// ──────────────────────────────────────────────
// Azure Container App
// ──────────────────────────────────────────────

@description('Environment name (dev, test, prod)')
param environment string

@description('Azure region for the resource')
param location string

@description('Container Apps Environment resource ID')
param containerAppsEnvId string

@description('Container image to deploy (e.g. myacr.azurecr.io/app:latest)')
param containerImage string

@description('ACR login server (e.g. myacr.azurecr.io)')
param acrLoginServer string

@description('Resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('Key Vault URI for secret references')
param keyVaultUri string

@description('Client ID of the managed identity')
param managedIdentityClientId string

@description('Application Insights connection string')
param applicationInsightsConnectionString string = ''

@description('Admin seed password')
@secure()
param adminSeedPassword string

@description('Candidate seed password')
@secure()
param candidateSeedPassword string

@description('JWT signing key')
@secure()
param jwtKey string

var appName = 'cloudsoft-${environment}-app'

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        transport: 'auto'
      }
      registries: [
        {
          server: acrLoginServer
          identity: managedIdentityId
        }
      ]
      secrets: [
        {
          name: 'admin-seed-password'
          value: adminSeedPassword
        }
        {
          name: 'candidate-seed-password'
          value: candidateSeedPassword
        }
        {
          name: 'jwt-key'
          value: jwtKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'recruitment-api'
          image: containerImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentityClientId
            }
            {
              name: 'KeyVault__VaultUri'
              value: keyVaultUri
            }
            {
              name: 'FeatureFlags__UseMongoDB'
              value: 'true'
            }
            {
              name: 'FeatureFlags__UseBlobStorage'
              value: 'true'
            }
            {
              name: 'FeatureFlags__UseGoogleAuth'
              value: 'false'
            }
            {
              name: 'FeatureFlags__UseKeyVault'
              value: 'true'
            }
            {
              name: 'FeatureFlags__UseApplicationInsights'
              value: 'true'
            }
            {
              name: 'FeatureFlags__UseRestCountries'
              value: 'true'
            }
            {
              name: 'MongoDb__ConnectionString'
              value: 'overridden-by-keyvault'
            }
            {
              name: 'MongoDb__DatabaseName'
              value: 'cloudsoft-recruitment'
            }
            {
              name: 'ConnectionStrings__Identity'
              value: 'Data Source=/app/data/identity.db'
            }
            {
              name: 'BlobStorage__ConnectionString'
              value: 'overridden-by-keyvault'
            }
            {
              name: 'BlobStorage__ContainerName'
              value: 'cvs'
            }
            {
              name: 'AdminSeed__Password'
              secretRef: 'admin-seed-password'
            }
            {
              name: 'CandidateSeed__Password'
              secretRef: 'candidate-seed-password'
            }
            {
              name: 'Jwt__Key'
              secretRef: 'jwt-key'
            }
            {
              name: 'Jwt__Issuer'
              value: 'CloudSoft'
            }
            {
              name: 'Jwt__Audience'
              value: 'CloudSoftApi'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
            }
            {
              name: 'ApplicationInsights__ConnectionString'
              value: applicationInsightsConnectionString
            }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 5
              failureThreshold: 30
              timeoutSeconds: 3
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 15
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '50'
              }
            }
          }
        ]
      }
    }
  }
}

@description('The Container App FQDN')
output fqdn string = containerApp.properties.configuration.ingress.fqdn

@description('The Container App name')
output name string = containerApp.name

@description('The Container App resource ID')
output id string = containerApp.id

@description('The Container App latest revision FQDN')
output latestRevisionFqdn string = containerApp.properties.latestRevisionFqdn
