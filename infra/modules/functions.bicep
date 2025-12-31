// Function Apps Module
// This module creates API and Batch Function Apps with environment-specific configurations,
// Consumption Plan for dev and Premium Plan for prod

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

@description('Storage Account name for Functions runtime')
param storageAccountName string

@description('Application Insights connection string')
param appInsightsConnectionString string = ''

@description('Application Insights instrumentation key')
param appInsightsInstrumentationKey string = ''

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

// Function App Naming: func-{project}-{resource}-{env}-{location}
var apiFunctionAppName = 'func-${projectName}-api-${environmentName}-${locationShort}'
var batchFunctionAppName = 'func-${projectName}-batch-${environmentName}-${locationShort}'

// App Service Plan Naming: plan-{project}-{env}-{location}
var appServicePlanName = 'plan-${projectName}-${environmentName}-${locationShort}'

// Application Insights Naming: appi-{project}-{env}-{location}
var appInsightsName = 'appi-${projectName}-${environmentName}-${locationShort}'

// Environment-specific App Service Plan configuration
// Note: 厳しいクォータ制限環境での段階的対応
// 1. まずwestus2リージョンで試行
// 2. すべてのVMクォータが0の場合は Container Apps への移行を検討
var planConfig = {
  dev: {
    sku: {
      name: 'Y1'  // Consumption Plan - サーバーレス（VMクォータ影響最小）
      tier: 'Dynamic'
    }
    kind: 'functionapp'
  }
  prod: {
    sku: {
      name: 'Y1'  // Consumption Plan - VMクォータが厳しい環境での最後の手段
      tier: 'Dynamic'
    }
    kind: 'functionapp'
  }
}

// Application Insights (if not provided)
resource appInsights 'Microsoft.Insights/components@2020-02-02' = if (empty(appInsightsConnectionString)) {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: environmentName == 'dev' ? 30 : 90
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: planConfig[environmentName].sku
  kind: planConfig[environmentName].kind
  properties: {
    reserved: true  // Required for Linux
  }
}

// Get storage account key for connection string
// Note: AzureWebJobsStorage requires a connection string for the Functions runtime
// Application code uses Managed Identity via StorageAccountName setting
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  name: storageAccountName
}

var storageAccountConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

// API Function App
resource apiFunctionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: apiFunctionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    reserved: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      alwaysOn: false  // Always On not supported in Consumption Plan
      use32BitWorkerProcess: false
      cors: {
        allowedOrigins: environmentName == 'dev' ? [
          '*'  // Allow all origins for development
        ] : [
          'https://*.azurestaticapps.net'  // Restrict to SWA in production
          'https://cdn-comical-prod-jpe.azureedge.net'  // CDN endpoint
        ]
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccountConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageAccountConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(apiFunctionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: !empty(appInsightsConnectionString) ? appInsightsConnectionString : (empty(appInsightsConnectionString) ? appInsights!.properties.ConnectionString : appInsightsConnectionString)
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${postgresConnectionStringSecretUri})'
        }
        {
          name: 'StorageAccountName'
          value: storageAccountName
        }
      ]
    }
  }
}

// Batch Function App (Durable Functions)
resource batchFunctionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: batchFunctionAppName
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    reserved: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      alwaysOn: false  // Always On not supported in Consumption Plan
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageAccountConnectionString
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: storageAccountConnectionString
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(batchFunctionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: !empty(appInsightsConnectionString) ? appInsightsConnectionString : (empty(appInsightsConnectionString) ? appInsights!.properties.ConnectionString : appInsightsConnectionString)
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'DefaultConnection'
          value: '@Microsoft.KeyVault(SecretUri=${postgresConnectionStringSecretUri})'
        }
        {
          name: 'StorageAccountName'
          value: storageAccountName
        }
        {
          name: 'RakutenBooksApi__applicationid'
          value: !empty(rakutenApiKeySecretUri) ? '@Microsoft.KeyVault(SecretUri=${rakutenApiKeySecretUri})' : ''
        }
      ]
    }
  }
}

// Outputs
output appServicePlanId string = appServicePlan.id
output appServicePlanName string = appServicePlan.name
output appServicePlanSku string = '${planConfig[environmentName].sku.tier}/${planConfig[environmentName].sku.name}'

output apiFunctionAppId string = apiFunctionApp.id
output apiFunctionAppName string = apiFunctionApp.name
output apiFunctionAppPrincipalId string = apiFunctionApp.identity.principalId
output apiFunctionAppHostname string = apiFunctionApp.properties.defaultHostName

output batchFunctionAppId string = batchFunctionApp.id
output batchFunctionAppName string = batchFunctionApp.name
output batchFunctionAppPrincipalId string = batchFunctionApp.identity.principalId
output batchFunctionAppHostname string = batchFunctionApp.properties.defaultHostName

output appInsightsId string = !empty(appInsightsConnectionString) ? '' : appInsights!.id
output appInsightsName string = !empty(appInsightsConnectionString) ? '' : appInsights!.name
output appInsightsConnectionString string = !empty(appInsightsConnectionString) ? appInsightsConnectionString : (empty(appInsightsConnectionString) ? appInsights!.properties.ConnectionString : '')
output appInsightsInstrumentationKey string = !empty(appInsightsConnectionString) ? appInsightsInstrumentationKey : (empty(appInsightsConnectionString) ? appInsights!.properties.InstrumentationKey : '')
