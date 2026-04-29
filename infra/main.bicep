// ============================================================================
// ComiCal - main.bicep (subscription scope orchestrator)
// ============================================================================
// Creates the resource group and orchestrates child modules:
//   - network       (placeholder; reserved for future Private Endpoints)
//   - data          (Azure SQL serverless + Storage account)
//   - app           (SWA + Functions API/Batch + Key Vault + App Configuration)
//   - observability (Log Analytics + App Insights + Action Group + Alerts)
// ----------------------------------------------------------------------------

targetScope = 'subscription'

@description('Common short prefix for all resource names (e.g. cmcl).')
@minLength(2)
@maxLength(6)
param prefix string

@description('Environment short code (dev / prod).')
@allowed([
  'dev'
  'prod'
])
param env string

@description('Azure region (e.g. japaneast). Single-region: Japan East only.')
param region string = 'japaneast'

@description('Short region code embedded in resource names (e.g. jpe).')
param regionShort string = 'jpe'

// ---------- SQL parameters ----------------------------------------------------

@description('Azure SQL serverless SKU tier. dev=GP_S_Gen5_1, prod=GP_S_Gen5_2.')
param sqlTier string = 'GP_S_Gen5_1'

@description('Azure SQL auto-pause delay in minutes (-1 disables).')
param sqlAutoPauseDelayMinutes int = 60

@description('Azure SQL administrator login (SQL auth fallback; primary auth is Entra/MI).')
param sqlAdminLogin string = 'comicaladmin'

@secure()
@description('Azure SQL administrator password (only used if Entra-only auth is not enforced).')
param sqlAdminPassword string

@description('Object ID of the Entra ID user/group to set as SQL AAD admin.')
param sqlAadAdminObjectId string = ''

@description('Display name of the Entra ID user/group to set as SQL AAD admin.')
param sqlAadAdminLogin string = ''

// ---------- Observability parameters -----------------------------------------

@description('Log Analytics retention in days. dev=30, prod=90.')
@minValue(30)
@maxValue(730)
param logRetentionDays int = 30

@secure()
@description('Slack/Teams Webhook URL for the Action Group (notification endpoint).')
param alertWebhookUrl string = ''

@description('Email addresses for the Action Group. Empty array allowed for dev (portal-only). prod environments must supply at least one address.')
param actionGroupEmails array = []

// ---------- Alert thresholds (per-env via bicepparam) ------------------------

@description('API failed requests count threshold over 5 min.')
@minValue(0)
param apiFailedRequestsThreshold int = 10

@description('API p95 request duration threshold (ms) over 15 min.')
@minValue(100)
param apiP95LatencyMs int = 2000

@description('SQL dependency failures count threshold over 5 min.')
@minValue(0)
param sqlDependencyFailuresThreshold int = 5

@description('Storage availability percentage threshold (alert when below) over 1 hour.')
@minValue(50)
@maxValue(100)
param storageAvailabilityPercent int = 99

@description('Rakuten 429 rate-limited count threshold over 5 min.')
@minValue(0)
param rakutenRateLimitedThreshold int = 50

// ---------- Network switch ---------------------------------------------------

@description('Reserved switch to enable Private Endpoints in the future. MVP=false.')
param privateEndpointEnabled bool = false

// ---------- Tags --------------------------------------------------------------

@description('Common tags applied to every deployed resource.')
param tags object = {
  project: 'ComiCal'
  env: env
  managedBy: 'Bicep'
}

// ----------------------------------------------------------------------------
// Resource Group
// ----------------------------------------------------------------------------

var rgName = '${prefix}-${env}-${regionShort}-rg'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: rgName
  location: region
  tags: tags
}

// ----------------------------------------------------------------------------
// Modules
// ----------------------------------------------------------------------------

module network 'modules/network.bicep' = {
  name: 'network-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    regionShort: regionShort
    location: region
    privateEndpointEnabled: privateEndpointEnabled
    tags: tags
  }
}

module observability 'modules/observability.bicep' = {
  name: 'observability-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    regionShort: regionShort
    location: region
    logRetentionDays: logRetentionDays
    actionGroupEmails: actionGroupEmails
    alertWebhookUrl: alertWebhookUrl
    tags: tags
  }
}

module data 'modules/data.bicep' = {
  name: 'data-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    regionShort: regionShort
    location: region
    sqlTier: sqlTier
    sqlAutoPauseDelayMinutes: sqlAutoPauseDelayMinutes
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    sqlAadAdminObjectId: sqlAadAdminObjectId
    sqlAadAdminLogin: sqlAadAdminLogin
    logAnalyticsWorkspaceId: observability.outputs.logAnalyticsWorkspaceId
    tags: tags
  }
}

module app 'modules/app.bicep' = {
  name: 'app-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    regionShort: regionShort
    location: region
    storageAccountName: data.outputs.storageAccountName
    sqlServerFqdn: data.outputs.sqlServerFqdn
    sqlDatabaseName: data.outputs.sqlDatabaseName
    appInsightsConnectionString: observability.outputs.appInsightsConnectionString
    logAnalyticsWorkspaceId: observability.outputs.logAnalyticsWorkspaceId
    tags: tags
  }
}

// ----------------------------------------------------------------------------
// Outputs (consumed by CI/CD pipelines)
// ----------------------------------------------------------------------------

@description('Resource group name created by this deployment.')
output resourceGroupName string = rg.name

@description('Static Web App resource name.')
output staticWebAppName string = app.outputs.staticWebAppName

@description('Function App (API) resource name.')
output functionAppApiName string = app.outputs.functionAppApiName

@description('Function App (Batch) resource name.')
output functionAppBatchName string = app.outputs.functionAppBatchName

@description('Key Vault resource name.')
output keyVaultName string = app.outputs.keyVaultName

@description('App Configuration resource name.')
output appConfigName string = app.outputs.appConfigName

@description('Azure SQL logical server name.')
output sqlServerName string = data.outputs.sqlServerName

@description('Azure SQL database name.')
output sqlDatabaseName string = data.outputs.sqlDatabaseName

@description('Storage account name.')
output storageAccountName string = data.outputs.storageAccountName

@description('Application Insights resource name.')
output appInsightsName string = observability.outputs.appInsightsName

@description('Log Analytics workspace name.')
output logAnalyticsWorkspaceName string = observability.outputs.logAnalyticsWorkspaceName

@description('System-assigned principal ID of the API Function App.')
output functionAppApiPrincipalId string = app.outputs.functionAppApiPrincipalId

@description('System-assigned principal ID of the Batch Function App.')
output functionAppBatchPrincipalId string = app.outputs.functionAppBatchPrincipalId

@description('System-assigned principal ID of the Static Web App.')
output staticWebAppPrincipalId string = app.outputs.staticWebAppPrincipalId

// ----------------------------------------------------------------------------
// Alerts & Workbook (depend on observability + data)
// ----------------------------------------------------------------------------

module alerts 'modules/alerts.bicep' = {
  name: 'alerts-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    regionShort: regionShort
    location: region
    appInsightsId: observability.outputs.appInsightsId
    storageAccountId: data.outputs.storageAccountId
    actionGroupId: observability.outputs.actionGroupId
    apiFailedRequestsThreshold: apiFailedRequestsThreshold
    apiP95LatencyMs: apiP95LatencyMs
    sqlDependencyFailuresThreshold: sqlDependencyFailuresThreshold
    storageAvailabilityPercent: storageAvailabilityPercent
    rakutenRateLimitedThreshold: rakutenRateLimitedThreshold
    tags: tags
  }
}

module workbook 'modules/workbook.bicep' = {
  name: 'workbook-deployment'
  scope: rg
  params: {
    env: env
    location: region
    appInsightsId: observability.outputs.appInsightsId
    tags: tags
  }
}

@description('IDs of every alert rule deployed.')
output alertRuleIds array = alerts.outputs.alertRuleIds

@description('Application Insights workbook resource ID.')
output workbookId string = workbook.outputs.workbookId
