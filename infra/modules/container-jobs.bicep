// Container Jobs Module - Scheduled batch processing with Container Apps Jobs
// This module creates Container Jobs for scheduled batch processing and a manual execution Container App

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

@description('Existing Container Apps Environment ID (optional - if not provided, creates new one)')
param existingContainerAppsEnvironmentId string = ''

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

// Container Jobs Naming: cjob-{project}-{resource}-{env}-{location}
var dataRegistrationJobName = 'cjob-${projectName}-datareg-${environmentName}-${locationShort}'
var imageDownloadJobName = 'cjob-${projectName}-imgdl-${environmentName}-${locationShort}'

// Manual execution Container App Naming: ca-{project}-{resource}-{env}-{location}
var manualBatchContainerAppName = 'ca-${projectName}-manualbatch-${environmentName}-${locationShort}'

// Container Apps Environment Naming: cae-{project}-{env}-{location}
var containerAppsEnvironmentName = 'cae-${projectName}-${environmentName}-${locationShort}'

// Log Analytics Workspace Naming: law-{project}-{env}-{location}
var logAnalyticsWorkspaceName = 'law-${projectName}-${environmentName}-${locationShort}'

// Log Analytics Workspace (only if not using existing Container Apps Environment)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = if (empty(existingContainerAppsEnvironmentId)) {
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

// Container Apps Environment (only if not using existing one)
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = if (empty(existingContainerAppsEnvironmentId)) {
  name: containerAppsEnvironmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace!.properties.customerId
        sharedKey: logAnalyticsWorkspace!.listKeys().primarySharedKey
      }
    }
  }
}

// Reference to existing Container Apps Environment (if provided)
resource existingContainerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = if (!empty(existingContainerAppsEnvironmentId)) {
  name: last(split(existingContainerAppsEnvironmentId, '/'))
}

// Use either the existing or newly created Container Apps Environment
var effectiveContainerAppsEnvironmentId = !empty(existingContainerAppsEnvironmentId) ? existingContainerAppsEnvironmentId : containerAppsEnvironment.id

// Get storage account key for connection string
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

// Data Registration Container Job (Scheduled: Daily UTC 0:00, Timeout: 4 hours)
resource dataRegistrationJob 'Microsoft.App/jobs@2024-03-01' = {
  name: dataRegistrationJobName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: effectiveContainerAppsEnvironmentId
    configuration: {
      scheduleTriggerConfig: {
        cronExpression: '0 0 * * *' // Daily at UTC 0:00
        parallelism: 1
        replicaCompletionCount: 1
      }
      replicaTimeout: 14400 // 4 hours in seconds
      replicaRetryLimit: 1
      triggerType: 'Schedule'
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
          name: 'data-registration'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
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
            {
              name: 'BATCH_JOB_TYPE'
              value: 'DataRegistration'
            }
          ]
        }
      ]
    }
  }
}

// Image Download Container Job (Scheduled: Daily UTC 4:00, Timeout: 4 hours)
resource imageDownloadJob 'Microsoft.App/jobs@2024-03-01' = {
  name: imageDownloadJobName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: effectiveContainerAppsEnvironmentId
    configuration: {
      scheduleTriggerConfig: {
        cronExpression: '0 4 * * *' // Daily at UTC 4:00
        parallelism: 1
        replicaCompletionCount: 1
      }
      replicaTimeout: 14400 // 4 hours in seconds
      replicaRetryLimit: 1
      triggerType: 'Schedule'
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
          name: 'image-download'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
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
            {
              name: 'BATCH_JOB_TYPE'
              value: 'ImageDownload'
            }
          ]
        }
      ]
    }
  }
}

// Manual Execution Container App (HTTP Trigger)
resource manualBatchContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: manualBatchContainerAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: effectiveContainerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true // External access for manual execution
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
          name: 'manual-batch'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
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
        minReplicas: 0 // Scale to zero when not in use
        maxReplicas: 1
      }
    }
  }
}

// Outputs
output containerAppsEnvironmentId string = effectiveContainerAppsEnvironmentId
output containerAppsEnvironmentName string = !empty(existingContainerAppsEnvironmentId) ? existingContainerAppsEnvironment.name : containerAppsEnvironment.name
output dataRegistrationJobId string = dataRegistrationJob.id
output dataRegistrationJobName string = dataRegistrationJob.name
output dataRegistrationJobPrincipalId string = dataRegistrationJob.identity.principalId
output imageDownloadJobId string = imageDownloadJob.id
output imageDownloadJobName string = imageDownloadJob.name
output imageDownloadJobPrincipalId string = imageDownloadJob.identity.principalId
output manualBatchContainerAppId string = manualBatchContainerApp.id
output manualBatchContainerAppName string = manualBatchContainerApp.name
output manualBatchContainerAppUrl string = 'https://${manualBatchContainerApp.properties.configuration.ingress.fqdn}'
output manualBatchContainerAppPrincipalId string = manualBatchContainerApp.identity.principalId
output logAnalyticsWorkspaceId string = !empty(existingContainerAppsEnvironmentId) ? '' : logAnalyticsWorkspace.id
