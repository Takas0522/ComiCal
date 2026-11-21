targetScope = 'subscription'

// ComiCal is deployed as a single production environment (the former Stage/dev environment
// was retired for cost reasons — see docs/adr/0004-azure-cost-reduction.md).
// The daily collection batch runs on the maintainer's PC (tools/run-batch-local.ps1), so no
// batch Function App, VNet or NAT Gateway is provisioned in Azure.

@description('Environment short code (only prod is deployed)')
@allowed([
  'prod'
])
param env string

@description('Azure region for all resources')
param location string = 'japaneast'

@description('Resource name prefix following CAF convention')
param prefix string = 'cmcl'

@description('Max SQL vCores for the Serverless tier (the free offer allows up to 4)')
param sqlVCores int = 2

@description('Create/keep the SQL database on the Azure SQL Database free offer (100,000 vCore-seconds and 32 GB per month). An existing paid database cannot be converted — migrate first (plan/scripts/apply-07-sql-free-offer.ps1).')
param sqlUseFreeLimit bool = true

@description('SQL Server administrator login name')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL Server administrator password — supply via CI/CD secret, not bicepparam')
param sqlAdminPassword string

@secure()
@description('Rakuten Books API application ID — stored in Key Vault for the locally run batch')
param rakutenApplicationId string

@secure()
@description('Rakuten Books API access key — stored in Key Vault for the locally run batch')
param rakutenAccessKey string

@secure()
@description('Rakuten affiliate ID — stored in Key Vault for the locally run batch')
param rakutenAffiliateId string

@description('Log Analytics workspace retention in days')
param logRetentionDays int = 30

@description('Log Analytics daily ingestion cap in GB (cost guard-rail)')
param logDailyQuotaGb string = '0.2'

@description('Enable Key Vault purge protection')
param enablePurgeProtection bool = true

@description('Webhook URL for alert notifications (Slack or Teams); leave empty to disable')
param alertWebhookUrl string = ''

@description('Email address for alert and budget notifications; leave empty to disable email receiver')
param alertEmailAddress string = ''

@description('Monthly cost budget for the resource group in the billing currency (JPY)')
param monthlyBudgetAmount int = 3000

@description('First day of the month the budget starts (yyyy-MM-01). Must not change after the budget is created.')
param budgetStartDate string = '2026-11-01'

@description('Entra object ID of the person who runs the batch locally; grants Blob write and Key Vault secret read. Leave empty to skip.')
param batchOperatorPrincipalId string = ''

@description('Reserved: enable private endpoints for future VNet integration (currently no-op)')
#disable-next-line no-unused-params
param privateEndpointEnabled bool = false

var rgName = '${prefix}-${env}-jpe-rg'

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: rgName
  location: location
}

module observability 'modules/observability.bicep' = {
  name: 'observability-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    location: location
    logRetentionDays: logRetentionDays
    logDailyQuotaGb: logDailyQuotaGb
    alertWebhookUrl: alertWebhookUrl
    alertEmailAddress: alertEmailAddress
  }
}

module data 'modules/data.bicep' = {
  name: 'data-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    location: location
    sqlVCores: sqlVCores
    sqlUseFreeLimit: sqlUseFreeLimit
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
  }
  dependsOn: [
    observability
  ]
}

module app 'modules/app.bicep' = {
  name: 'app-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    location: location
    sqlServerFqdn: data.outputs.sqlServerFqdn
    sqlDatabaseName: data.outputs.sqlDatabaseName
    storageAccountName: data.outputs.storageAccountName
    storageAccountId: data.outputs.storageAccountId
    appInsightsName: observability.outputs.appInsightsName
    enablePurgeProtection: enablePurgeProtection
    rakutenApplicationId: rakutenApplicationId
    rakutenAccessKey: rakutenAccessKey
    rakutenAffiliateId: rakutenAffiliateId
  }
}

module batchOperator 'modules/batch-operator.bicep' = if (!empty(batchOperatorPrincipalId)) {
  name: 'batch-operator-deployment'
  scope: rg
  params: {
    storageAccountName: data.outputs.storageAccountName
    keyVaultName: app.outputs.kvName
    principalId: batchOperatorPrincipalId
  }
}

module budget 'modules/budget.bicep' = {
  name: 'budget-deployment'
  scope: rg
  params: {
    prefix: prefix
    env: env
    amount: monthlyBudgetAmount
    startDate: budgetStartDate
    actionGroupId: observability.outputs.actionGroupId
    contactEmails: empty(alertEmailAddress) ? [] : [
      alertEmailAddress
    ]
  }
}

@description('Deployed resource group name')
output resourceGroupName string = rg.name

@description('Static Web App default hostname')
output swaDefaultHostname string = app.outputs.swaDefaultHostname

@description('Function App (API) name')
output funcApiName string = app.outputs.funcApiName

@description('Key Vault name (the local batch reads Rakuten secrets from here)')
output kvName string = app.outputs.kvName

@description('Key Vault URI')
output kvUri string = app.outputs.kvUri

@description('Storage account name used by the Function App and the local batch')
output storageAccountName string = data.outputs.storageAccountName

@description('SQL Server fully qualified domain name')
output sqlServerFqdn string = data.outputs.sqlServerFqdn

@description('SQL database name')
output sqlDatabaseName string = data.outputs.sqlDatabaseName
