@description('Resource name prefix following CAF convention')
param prefix string

@description('Environment short code (dev or prod)')
param env string

@description('Azure region')
param location string

@description('SQL Server fully-qualified domain name (from data module)')
param sqlServerFqdn string

@description('SQL Database name (from data module)')
param sqlDatabaseName string

@description('Storage account name (from data module)')
param storageAccountName string

@description('Storage account resource ID used to retrieve access keys for Key Vault secret (from data module)')
param storageAccountId string

@description('Application Insights resource name (from observability module); used to obtain connection string via existing reference')
param appInsightsName string

@description('Enable Key Vault purge protection (false for dev, true for prod)')
param enablePurgeProtection bool

var kvName = '${prefix}-${env}-jpe-kv'
var swaName = '${prefix}-${env}-jpe-swa'
var funcApiName = '${prefix}-${env}-jpe-func-api'
var funcBatchName = '${prefix}-${env}-jpe-func-batch'
var planApiName = '${prefix}-${env}-jpe-plan-api'
var planBatchName = '${prefix}-${env}-jpe-plan-batch'
var appConfigName = '${prefix}-${env}-jpe-appcfg'

// Role definition ID for "Key Vault Secrets User"
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// SQL connection string using Managed Identity — no password required
var sqlConnectionString = 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

// Storage connection string stored in Key Vault; Function Apps access it via KV reference
var storageKey = listKeys(storageAccountId, '2023-05-01').keys[0].value
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageKey};EndpointSuffix=core.windows.net'

// Reference existing App Insights created by observability module to obtain its connection string
// without exposing it as a module output (secrets must not pass through outputs per policy)
resource existingAppInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

// ──────────────────────────────────────────────────────────────────────────────
// Key Vault
// ──────────────────────────────────────────────────────────────────────────────

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // RBAC mode — access policies are replaced by Azure role assignments
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: enablePurgeProtection
  }
}

// Store storage connection string as a Key Vault secret
resource kvSecretStorage 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'AzureWebJobsStorage'
  properties: {
    value: storageConnectionString
  }
}

// Store App Insights connection string as a Key Vault secret
resource kvSecretAppInsights 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'AppInsightsConnectionString'
  properties: {
    value: existingAppInsights.properties.ConnectionString
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// App Service Plans (Consumption, Linux) — API and Batch use separate plans
// to ensure independent scaling and deployment
// ──────────────────────────────────────────────────────────────────────────────

resource planApi 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planApiName
  location: location
  kind: 'linux'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

resource planBatch 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planBatchName
  location: location
  kind: 'linux'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  properties: {
    reserved: true
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// Function App — API (SWA-linked)
// ──────────────────────────────────────────────────────────────────────────────

resource funcApi 'Microsoft.Web/sites@2024-04-01' = {
  name: funcApiName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planApi.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'AzureWebJobsStorage'
          value: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/AzureWebJobsStorage/)'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/AppInsightsConnectionString/)'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: sqlConnectionString
        }
        {
          name: 'AppConfiguration__Endpoint'
          value: 'https://${appConfigName}.azconfig.io'
        }
      ]
    }
  }
  dependsOn: [
    kvSecretStorage
    kvSecretAppInsights
  ]
}

// ──────────────────────────────────────────────────────────────────────────────
// Function App — Batch (Durable Functions, Consumption, separate plan)
// ──────────────────────────────────────────────────────────────────────────────

resource funcBatch 'Microsoft.Web/sites@2024-04-01' = {
  name: funcBatchName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planBatch.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'AzureWebJobsStorage'
          value: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/AzureWebJobsStorage/)'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/AppInsightsConnectionString/)'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: sqlConnectionString
        }
        {
          name: 'AppConfiguration__Endpoint'
          value: 'https://${appConfigName}.azconfig.io'
        }
      ]
    }
  }
  dependsOn: [
    kvSecretStorage
    kvSecretAppInsights
  ]
}

// ──────────────────────────────────────────────────────────────────────────────
// Key Vault RBAC — grant "Key Vault Secrets User" to each Function App MI and SWA MI
// ──────────────────────────────────────────────────────────────────────────────

resource kvRoleAssignFuncApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, funcApi.id, kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: funcApi.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource kvRoleAssignFuncBatch 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, funcBatch.id, kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: funcBatch.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// Static Web App (Standard) — hosts Angular SSR via Managed Functions
// ──────────────────────────────────────────────────────────────────────────────

resource swa 'Microsoft.Web/staticSites@2024-04-01' = {
  name: swaName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

// Link the API Function App to the SWA so that /api/* requests are proxied
// with the x-ms-client-principal header injected by SWA authentication
resource swaLinkedBackend 'Microsoft.Web/staticSites/linkedBackends@2024-04-01' = {
  parent: swa
  name: 'apifunc'
  properties: {
    backendResourceId: funcApi.id
    region: location
  }
}

resource kvRoleAssignSwa 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, swa.id, kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: swa.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// App Configuration (Standard) — feature flags
// ──────────────────────────────────────────────────────────────────────────────

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: appConfigName
  location: location
  sku: {
    name: 'standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    disableLocalAuth: false
    enablePurgeProtection: false
  }
}

// Feature flags are stored as key-values with the .appconfig.featureflag/ prefix.
// The slash in the key is encoded as ~2F in the ARM resource name.
// All flags start as disabled (false) and are enabled via staged rollout.

resource ffDiscoveryRecommend 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: '.appconfig.featureflag~2Fdiscovery-recommend'
  properties: {
    value: '{"id":"discovery-recommend","description":"Recommendation panel on discovery page","enabled":false,"conditions":{"client_filters":[]}}'
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource ffCalendarAbTest 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: '.appconfig.featureflag~2Fcalendar-ab-test'
  properties: {
    value: '{"id":"calendar-ab-test","description":"A/B test for calendar view layout","enabled":false,"conditions":{"client_filters":[]}}'
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

resource ffEntraLoginRollout 'Microsoft.AppConfiguration/configurationStores/keyValues@2023-03-01' = {
  parent: appConfig
  name: '.appconfig.featureflag~2Fentra-login-rollout'
  properties: {
    value: '{"id":"entra-login-rollout","description":"Gradual rollout of Entra External ID login","enabled":false,"conditions":{"client_filters":[]}}'
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// Outputs
// ──────────────────────────────────────────────────────────────────────────────

@description('Static Web App default hostname')
output swaDefaultHostname string = swa.properties.defaultHostname

@description('Function App (API) resource name')
output funcApiName string = funcApi.name

@description('Function App (Batch) resource name')
output funcBatchName string = funcBatch.name

@description('Key Vault URI')
output kvUri string = kv.properties.vaultUri

@description('App Configuration endpoint')
output appConfigEndpoint string = appConfig.properties.endpoint
