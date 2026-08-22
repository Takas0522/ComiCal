targetScope = 'subscription'

@description('Environment short code (dev or prod)')
param env string

@description('Azure region for all resources')
param location string = 'japaneast'

@description('Resource name prefix following CAF convention')
param prefix string = 'cmcl'

@description('Number of SQL vCores for Serverless tier (1 for dev, 2 for prod)')
param sqlVCores int = 1

@description('SQL Server administrator login name')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL Server administrator password — supply via CI/CD secret, not bicepparam')
param sqlAdminPassword string

@secure()
@description('Rakuten Books API application ID — supply via CI/CD secret, not bicepparam')
param rakutenApplicationId string

@secure()
@description('Rakuten Books API access key — supply via CI/CD secret, not bicepparam')
param rakutenAccessKey string

@secure()
@description('Rakuten affiliate ID — supply via CI/CD secret, not bicepparam')
param rakutenAffiliateId string

@description('Log Analytics workspace retention in days (30 for dev, 90 for prod)')
param logRetentionDays int = 30

@description('Enable Key Vault purge protection (false for dev to allow easy teardown, true for prod)')
param enablePurgeProtection bool = true

@description('Webhook URL for alert notifications (Slack or Teams); leave empty to disable')
param alertWebhookUrl string = ''

@description('Email address for alert notifications; leave empty to disable email receiver')
param alertEmailAddress string = ''

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

@description('Deployed resource group name')
output resourceGroupName string = rg.name

@description('Static Web App default hostname')
output swaDefaultHostname string = app.outputs.swaDefaultHostname

@description('Function App (API) name')
output funcApiName string = app.outputs.funcApiName

@description('Function App (Batch) name')
output funcBatchName string = app.outputs.funcBatchName

@description('Key Vault URI')
output kvUri string = app.outputs.kvUri

@description('App Configuration endpoint')
output appConfigEndpoint string = app.outputs.appConfigEndpoint

@description('SQL Server fully qualified domain name')
output sqlServerFqdn string = data.outputs.sqlServerFqdn

@description('SQL database name')
output sqlDatabaseName string = data.outputs.sqlDatabaseName
