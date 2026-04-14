targetScope = 'resourceGroup'

param location string = resourceGroup().location

@secure()
@description('Admin seed password')
param adminSeedPassword string

@secure()
@description('Candidate seed password')
param candidateSeedPassword string

@secure()
@description('JWT signing key (at least 32 characters)')
param jwtKey string

var suffix = uniqueString(resourceGroup().id)

// Container Registry
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'cloudsoftstageacr${suffix}'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// Log Analytics (required by Container Apps Environment)
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'cloudsoft-stage-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// Container Apps Environment
resource containerAppsEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cloudsoft-stage-aca-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Container App (initial deployment with placeholder image)
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'cloudsoft-stage-app'
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnv.id
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
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
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
          name: 'cloudsoft-web'
          image: 'mcr.microsoft.com/dotnet/samples:aspnetapp'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Staging' }
            { name: 'FeatureFlags__UseMongoDB', value: 'false' }
            { name: 'FeatureFlags__UseBlobStorage', value: 'false' }
            { name: 'FeatureFlags__UseGoogleAuth', value: 'false' }
            { name: 'FeatureFlags__UseKeyVault', value: 'false' }
            { name: 'FeatureFlags__UseApplicationInsights', value: 'false' }
            { name: 'FeatureFlags__UseRestCountries', value: 'true' }
            { name: 'ConnectionStrings__Identity', value: 'Data Source=/app/data/identity.db' }
            { name: 'AdminSeed__Password', secretRef: 'admin-seed-password' }
            { name: 'CandidateSeed__Password', secretRef: 'candidate-seed-password' }
            { name: 'Jwt__Key', secretRef: 'jwt-key' }
            { name: 'Jwt__Issuer', value: 'CloudSoft' }
            { name: 'Jwt__Audience', value: 'CloudSoftApi' }
          ]
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 0
              periodSeconds: 5
              failureThreshold: 10
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
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output acrLoginServer string = acr.properties.loginServer
output acrName string = acr.name
output appFqdn string = containerApp.properties.configuration.ingress.fqdn
output appUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output containerAppName string = containerApp.name
output resourceGroupName string = resourceGroup().name
