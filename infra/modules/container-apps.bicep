// Container Apps Module (Alternative to Function Apps for quota-limited subscriptions)
// This module creates Container Apps as an alternative when VM quotas are exhausted

@description('Environment name (dev, prod)')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Project name for resource naming')
param projectName string

@description('Tags to apply to resources')
param tags object = {}

@description('Storage Account name for runtime')
param storageAccountName string

@description('Application Insights connection string')
@secure()
param appInsightsConnectionString string = ''

@description('PostgreSQL connection string secret URI')
param postgresConnectionStringSecretUri string

@description('Rakuten API key secret URI')
param rakutenApiKeySecretUri string = ''

// Variables for naming conventions
var locationAbbreviation = {
  japaneast: 'jpe'
  japanwest: 'jpw'
  eastus: 'eus'
  eastus2: 'eu2'
  westus: 'wus'
  westus2: 'wu2'
  centralus: 'cus'
  eastasia: 'eas'
  southeastasia: 'sea'
}

var locationShort = locationAbbreviation[location]

// Container App Naming: ca-{project}-{resource}-{env}-{location}
var apiContainerAppName = 'ca-${projectName}-api-${environmentName}-${locationShort}'
var batchContainerAppName = 'ca-${projectName}-batch-${environmentName}-${locationShort}'

// Container Apps Environment Naming: cae-{project}-{env}-{location}
var containerAppsEnvironmentName = 'cae-${projectName}-${environmentName}-${locationShort}'

// Log Analytics Workspace Naming: law-{project}-{env}-{location}
var logAnalyticsWorkspaceName = 'law-${projectName}-${environmentName}-${locationShort}'

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environmentName == 'dev' ? 30 : 90
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Container Apps Environment
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

// Get storage account key for connection string
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

// API Container App
resource apiContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: apiContainerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      secrets: [
        {
          name: 'storage-connection-string'
          value: storageAccountConnectionString
        }
        {
          name: 'appinsights-connection-string'
          value: !empty(appInsightsConnectionString) ? appInsightsConnectionString : 'placeholder'
        }
      ]
    }
    template: {
      containers: [
        {
          image: 'mcr.microsoft.com/dotnet/aspnet:8.0'
          name: 'api'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'AzureWebJobsStorage'
              secretRef: 'storage-connection-string'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'appinsights-connection-string'
            }
            {
              name: 'DefaultConnection'
              value: postgresConnectionStringSecretUri
            }
            {
              name: 'RAKUTEN_API_KEY'
              value: rakutenApiKeySecretUri
            }
          ]
        }
      ]
      scale: {
        minReplicas: environmentName == 'dev' ? 0 : 1
        maxReplicas: environmentName == 'dev' ? 2 : 10
      }
    }
  }
}

// Batch Container App (シンプル化 - HTTPトリガーで手動実行可能)
resource batchContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: batchContainerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false // 内部のみアクセス可能
        targetPort: 8080
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      secrets: [
        {
          name: 'storage-connection-string'
          value: storageAccountConnectionString
        }
        {
          name: 'appinsights-connection-string'
          value: !empty(appInsightsConnectionString) ? appInsightsConnectionString : 'placeholder'
        }
      ]
    }
    template: {
      containers: [
        {
          image: 'mcr.microsoft.com/dotnet/aspnet:8.0'
          name: 'batch'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'AzureWebJobsStorage'
              secretRef: 'storage-connection-string'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              secretRef: 'appinsights-connection-string'
            }
            {
              name: 'DefaultConnection'
              value: postgresConnectionStringSecretUri
            }
            {
              name: 'RAKUTEN_API_KEY'
              value: rakutenApiKeySecretUri
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

// Outputs
output containerAppsEnvironmentId string = containerAppsEnvironment.id
output containerAppsEnvironmentName string = containerAppsEnvironment.name
output apiContainerAppId string = apiContainerApp.id
output apiContainerAppName string = apiContainerApp.name
output apiContainerAppUrl string = 'https://${apiContainerApp.properties.configuration.ingress.fqdn}'
output apiContainerAppPrincipalId string = apiContainerApp.identity.principalId
output batchContainerAppId string = batchContainerApp.id
output batchContainerAppName string = batchContainerApp.name
output batchContainerAppPrincipalId string = batchContainerApp.identity.principalId
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
