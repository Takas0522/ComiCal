// ============================================================================
// app.bicep - SWA + Function Apps (API/Batch) + Key Vault + App Configuration
// ----------------------------------------------------------------------------
// Resources:
//   - Key Vault (RBAC, soft-delete + purge protection)
//   - Placeholder secrets (so App Settings KV references resolve before
//     real values are populated post-deploy):
//        RAKUTEN-APPLICATION-ID
//        APPINSIGHTS-CONNECTION-STRING (seeded with the AppInsights value)
//        AADB2C-PROVIDER-CLIENT-SECRET
//   - App Configuration Standard with seed Feature Flags (all OFF)
//   - Static Web App Standard (system-assigned MI)
//   - Consumption hosting plan (Y1) shared by both Function Apps
//   - Function App (API)   - .NET 10 isolated, system-assigned MI
//   - Function App (Batch) - .NET 10 isolated, system-assigned MI
//   - SWA <-> Function App (API) `linkedBackend` link
//   - RBAC role assignments on Key Vault: `Key Vault Secrets User` for the
//     three managed identities (SWA, Func API, Func Batch)
// AVM note:
//   `avm/res/key-vault/vault`, `avm/res/web/static-site` and
//   `avm/res/web/site` are available, but inline authoring keeps the linked
//   backend, KV-reference App Settings, and three role assignments tight
//   and easy to audit.
// ============================================================================

@description('Common short prefix for all resource names.')
param prefix string

@description('Environment short code (dev / prod).')
param env string

@description('Short region code embedded in resource names.')
param regionShort string

@description('Azure region.')
param location string

@description('Storage account name (passed in from data module) used as Functions runtime storage.')
param storageAccountName string

@description('Azure SQL logical server FQDN (passed in from data module).')
param sqlServerFqdn string

@description('Azure SQL database name (passed in from data module).')
param sqlDatabaseName string

@description('Application Insights connection string (non-secret).')
param appInsightsConnectionString string

@description('Log Analytics workspace resource ID for diagnostics.')
param logAnalyticsWorkspaceId string

@description('Common tags applied to every deployed resource.')
param tags object = {}

// ----------------------------------------------------------------------------
// Names
// ----------------------------------------------------------------------------

var swaName    = '${prefix}-${env}-${regionShort}-swa'
var planName   = '${prefix}-${env}-${regionShort}-plan'
var funcApiName   = '${prefix}-${env}-${regionShort}-func-api'
var funcBatchName = '${prefix}-${env}-${regionShort}-func-batch'
var kvName     = '${prefix}-${env}-${regionShort}-kv'
var appCfgName = '${prefix}-${env}-${regionShort}-appcfg'

// Well-known role definition: Key Vault Secrets User
var roleKvSecretsUser = '4633458b-17de-408a-b874-0445c86b69e6'
// Well-known role definition: Storage Blob Data Owner (for Functions runtime)
var roleStorageBlobDataOwner = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'

// ----------------------------------------------------------------------------
// Existing storage account (created in data.bicep)
// ----------------------------------------------------------------------------

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' existing = {
  name: storageAccountName
}

// ----------------------------------------------------------------------------
// Key Vault
// ----------------------------------------------------------------------------

resource kv 'Microsoft.KeyVault/vaults@2024-11-01' = {
  name: kvName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// Placeholder secrets (populated post-deploy; created so that KV references
// in App Settings resolve and the Function Apps start cleanly).
resource secretRakuten 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: kv
  name: 'RAKUTEN-APPLICATION-ID'
  properties: {
    value: 'replace-after-deploy'
    contentType: 'text/plain'
  }
}

resource secretAppInsights 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: kv
  name: 'APPINSIGHTS-CONNECTION-STRING'
  properties: {
    value: appInsightsConnectionString
    contentType: 'text/plain'
  }
}

resource secretAadB2cClientSecret 'Microsoft.KeyVault/vaults/secrets@2024-11-01' = {
  parent: kv
  name: 'AADB2C-PROVIDER-CLIENT-SECRET'
  properties: {
    value: 'replace-after-deploy'
    contentType: 'text/plain'
  }
}

// ----------------------------------------------------------------------------
// App Configuration with seed Feature Flags (all OFF)
// ----------------------------------------------------------------------------

resource appCfg 'Microsoft.AppConfiguration/configurationStores@2024-06-01' = {
  name: appCfgName
  location: location
  tags: tags
  sku: {
    name: 'standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

var seedFeatureFlags = [
  'discovery.recommend'
  'discovery.trending'
  'sharing.og-card'
  'sharing.public-link'
  'auth.entra-external-id'
]

@batchSize(1)
resource featureFlags 'Microsoft.AppConfiguration/configurationStores/keyValues@2024-06-01' = [for flag in seedFeatureFlags: {
  parent: appCfg
  name: '.appconfig.featureflag~2F${flag}'
  properties: {
    value: string({
      id: flag
      description: 'Seeded by Bicep. Toggle in App Configuration.'
      enabled: false
      conditions: { client_filters: [] }
    })
    contentType: 'application/vnd.microsoft.appconfig.ff+json;charset=utf-8'
  }
}]

// ----------------------------------------------------------------------------
// Hosting plan (Consumption Y1) shared by both Function Apps
// ----------------------------------------------------------------------------

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {
    reserved: false
  }
}

// ----------------------------------------------------------------------------
// Common App Settings for Function Apps (KV references)
// ----------------------------------------------------------------------------

var kvRefRakuten         = '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/RAKUTEN-APPLICATION-ID/)'
var kvRefAppInsightsConn = '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/APPINSIGHTS-CONNECTION-STRING/)'
var kvRefAadB2cSecret    = '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/AADB2C-PROVIDER-CLIENT-SECRET/)'

var commonFunctionAppSettings = [
  { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
  { name: 'FUNCTIONS_WORKER_RUNTIME',    value: 'dotnet-isolated' }
  { name: 'WEBSITE_RUN_FROM_PACKAGE',    value: '1' }
  { name: 'AzureWebJobsStorage__accountName', value: storage.name }
  { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: kvRefAppInsightsConn }
  { name: 'RAKUTEN_APPLICATION_ID',                value: kvRefRakuten }
  { name: 'AADB2C_PROVIDER_CLIENT_SECRET',         value: kvRefAadB2cSecret }
  { name: 'APP_CONFIGURATION_ENDPOINT',            value: appCfg.properties.endpoint }
  { name: 'SQL_SERVER_FQDN',                       value: sqlServerFqdn }
  { name: 'SQL_DATABASE_NAME',                     value: sqlDatabaseName }
]

// ----------------------------------------------------------------------------
// Function App (API)
// ----------------------------------------------------------------------------

resource funcApi 'Microsoft.Web/sites@2024-04-01' = {
  name: funcApiName
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      netFrameworkVersion: 'v10.0'
      appSettings: concat(commonFunctionAppSettings, [
        { name: 'COMICAL_ROLE', value: 'api' }
      ])
    }
  }
}

// ----------------------------------------------------------------------------
// Function App (Batch)
// ----------------------------------------------------------------------------

resource funcBatch 'Microsoft.Web/sites@2024-04-01' = {
  name: funcBatchName
  location: location
  tags: tags
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      netFrameworkVersion: 'v10.0'
      appSettings: concat(commonFunctionAppSettings, [
        { name: 'COMICAL_ROLE', value: 'batch' }
      ])
    }
  }
}

// ----------------------------------------------------------------------------
// Static Web App (Standard)
// ----------------------------------------------------------------------------

resource swa 'Microsoft.Web/staticSites@2024-04-01' = {
  name: swaName
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    allowConfigFileUpdates: true
    stagingEnvironmentPolicy: 'Enabled'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

// SWA -> Function App (API) link (`linkedBackend`)
resource swaLinkedBackend 'Microsoft.Web/staticSites/linkedBackends@2024-04-01' = {
  parent: swa
  name: 'api'
  properties: {
    backendResourceId: funcApi.id
    region: location
  }
}

// SWA application settings — exposes APPLICATIONINSIGHTS_CONNECTION_STRING to the
// Managed Functions runtime (SSR) and to the build pipeline as AI_CONNECTION_STRING
// so the inline <script> bootstrap in index.html receives it. Both flow from the
// same Key Vault secret (read via the SWA system-assigned managed identity).
resource swaAppSettings 'Microsoft.Web/staticSites/config@2024-04-01' = {
  parent: swa
  name: 'appsettings'
  properties: {
    APPLICATIONINSIGHTS_CONNECTION_STRING: kvRefAppInsightsConn
    AI_CONNECTION_STRING: kvRefAppInsightsConn
  }
}

// ----------------------------------------------------------------------------
// Diagnostics
// ----------------------------------------------------------------------------

resource funcApiDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: funcApi
  name: 'func-api-diag'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      { category: 'FunctionAppLogs', enabled: true }
    ]
    metrics: [
      { category: 'AllMetrics', enabled: true }
    ]
  }
}

resource funcBatchDiag 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: funcBatch
  name: 'func-batch-diag'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      { category: 'FunctionAppLogs', enabled: true }
    ]
    metrics: [
      { category: 'AllMetrics', enabled: true }
    ]
  }
}

// ----------------------------------------------------------------------------
// RBAC role assignments
// ----------------------------------------------------------------------------

// Key Vault Secrets User -> Function App (API)
resource raKvFuncApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, funcApi.id, roleKvSecretsUser)
  properties: {
    principalId: funcApi.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleKvSecretsUser)
  }
}

// Key Vault Secrets User -> Function App (Batch)
resource raKvFuncBatch 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, funcBatch.id, roleKvSecretsUser)
  properties: {
    principalId: funcBatch.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleKvSecretsUser)
  }
}

// Key Vault Secrets User -> Static Web App
resource raKvSwa 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, swa.id, roleKvSecretsUser)
  properties: {
    principalId: swa.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleKvSecretsUser)
  }
}

// Storage Blob Data Owner -> Functions (so AzureWebJobsStorage__accountName works with MI)
resource raStorageFuncApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, funcApi.id, roleStorageBlobDataOwner)
  properties: {
    principalId: funcApi.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleStorageBlobDataOwner)
  }
}

resource raStorageFuncBatch 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, funcBatch.id, roleStorageBlobDataOwner)
  properties: {
    principalId: funcBatch.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleStorageBlobDataOwner)
  }
}

// ----------------------------------------------------------------------------
// Outputs
// ----------------------------------------------------------------------------

@description('Static Web App resource name.')
output staticWebAppName string = swa.name

@description('Static Web App default hostname.')
output staticWebAppHostname string = swa.properties.defaultHostname

@description('Static Web App system-assigned principal ID.')
output staticWebAppPrincipalId string = swa.identity.principalId

@description('Function App (API) resource name.')
output functionAppApiName string = funcApi.name

@description('Function App (API) default hostname.')
output functionAppApiHostname string = funcApi.properties.defaultHostName

@description('Function App (API) system-assigned principal ID.')
output functionAppApiPrincipalId string = funcApi.identity.principalId

@description('Function App (Batch) resource name.')
output functionAppBatchName string = funcBatch.name

@description('Function App (Batch) system-assigned principal ID.')
output functionAppBatchPrincipalId string = funcBatch.identity.principalId

@description('Key Vault resource name.')
output keyVaultName string = kv.name

@description('Key Vault DNS URI (e.g. https://<name>.vault.azure.net/).')
output keyVaultUri string = kv.properties.vaultUri

@description('App Configuration resource name.')
output appConfigName string = appCfg.name

@description('App Configuration endpoint.')
output appConfigEndpoint string = appCfg.properties.endpoint
