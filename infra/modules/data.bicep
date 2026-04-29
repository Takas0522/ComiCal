// ============================================================================
// data.bicep - Azure SQL Database (Serverless) + Storage Account
// ----------------------------------------------------------------------------
// Resources:
//   - SQL logical server (Microsoft.Sql/servers, Entra-aware)
//   - SQL database (GP_S_Gen5 serverless, auto-pause)
//   - Firewall rule allowing Azure services
//   - Diagnostic setting -> Log Analytics (passed in from observability module)
//   - Storage account (StorageV2, LRS, TLS1_2, no public blob access at account level)
//   - Blob containers: `covers` (public-read blob), `sync-tmp` (private)
//   - Queue: `dlq`
//   - Lifecycle rule: delete contents of `sync-tmp/` older than 1 day
//     (NOTE: Storage Lifecycle Management has a 1-day minimum granularity.
//      The original spec asked for 5 minutes; finer cleanup must be handled
//      in batch code. This rule provides a safety-net janitor.)
// AVM note:
//   AVM modules `avm/res/sql/server` and `avm/res/storage/storage-account`
//   exist, but we author inline here to keep tight control over the
//   serverless auto-pause settings, container/queue layout and lifecycle
//   policy without juggling many AVM parameters.
// ============================================================================

@description('Common short prefix for all resource names.')
@minLength(2)
@maxLength(6)
param prefix string

@description('Environment short code (dev / prod).')
@minLength(2)
@maxLength(8)
param env string

@description('Short region code embedded in resource names.')
@minLength(2)
@maxLength(6)
param regionShort string

@description('Azure region.')
param location string

@description('Azure SQL serverless SKU tier (e.g. GP_S_Gen5_1, GP_S_Gen5_2).')
param sqlTier string

@description('Azure SQL auto-pause delay in minutes. -1 disables auto-pause.')
param sqlAutoPauseDelayMinutes int

@description('Azure SQL administrator login (SQL auth fallback).')
param sqlAdminLogin string

@secure()
@description('Azure SQL administrator password.')
param sqlAdminPassword string

@description('Object ID of the Entra ID user/group set as SQL AAD admin (optional).')
param sqlAadAdminObjectId string = ''

@description('Display name of the Entra ID user/group set as SQL AAD admin (optional).')
param sqlAadAdminLogin string = ''

@description('Log Analytics workspace resource ID for diagnostics.')
param logAnalyticsWorkspaceId string

@description('Common tags applied to every deployed resource.')
param tags object = {}

// ----------------------------------------------------------------------------
// SQL Server + Database
// ----------------------------------------------------------------------------

var sqlServerName = '${prefix}-${env}-${regionShort}-sql'
var sqlDatabaseName = '${prefix}-${env}-${regionShort}-sqldb'

// Parse tier like 'GP_S_Gen5_1' -> family=Gen5, capacity=1, name=GP_S_Gen5
var tierParts = split(sqlTier, '_')
var skuFamily = tierParts[2]
var skuCapacity = int(tierParts[3])
var skuName = '${tierParts[0]}_${tierParts[1]}_${tierParts[2]}'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    version: '12.0'
    administrators: empty(sqlAadAdminObjectId) ? null : {
      administratorType: 'ActiveDirectory'
      principalType: 'Group'
      login: sqlAadAdminLogin
      sid: sqlAadAdminObjectId
      tenantId: tenant().tenantId
      azureADOnlyAuthentication: false
    }
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: 'GeneralPurpose'
    family: skuFamily
    capacity: skuCapacity
  }
  properties: {
    collation: 'Japanese_CI_AS'
    autoPauseDelay: sqlAutoPauseDelayMinutes
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368 // 32 GiB
    zoneRedundant: false
    readScale: 'Disabled'
    requestedBackupStorageRedundancy: 'Local'
  }
}

resource sqlDbDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: sqlDatabase
  name: 'sqldb-diag'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      { category: 'SQLInsights',          enabled: true }
      { category: 'AutomaticTuning',      enabled: true }
      { category: 'QueryStoreRuntimeStatistics', enabled: true }
      { category: 'QueryStoreWaitStatistics',    enabled: true }
      { category: 'Errors',               enabled: true }
      { category: 'DatabaseWaitStatistics', enabled: true }
      { category: 'Timeouts',             enabled: true }
      { category: 'Blocks',               enabled: true }
      { category: 'Deadlocks',            enabled: true }
    ]
    metrics: [
      { category: 'Basic',              enabled: true }
      { category: 'InstanceAndAppAdvanced', enabled: true }
      { category: 'WorkloadManagement', enabled: true }
    ]
  }
}

// ----------------------------------------------------------------------------
// Storage Account + Containers + Queue + Lifecycle Policy
// ----------------------------------------------------------------------------

// {prefix}{env}{regionShort}st  - dashes not allowed, max 24 chars
var storageAccountName = take(toLower(replace('${prefix}${env}${regionShort}st', '-', '')), 24)

resource storage 'Microsoft.Storage/storageAccounts@2024-01-01' = {
  #disable-next-line BCP334
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    // covers container needs blob-level public read; account-level access
    // must therefore be allowed (the container itself controls visibility).
    allowBlobPublicAccess: true
    allowSharedKeyAccess: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
    encryption: {
      services: {
        blob: { enabled: true, keyType: 'Account' }
        file: { enabled: true, keyType: 'Account' }
        queue: { enabled: true, keyType: 'Account' }
        table: { enabled: true, keyType: 'Account' }
      }
      keySource: 'Microsoft.Storage'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2024-01-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: { enabled: true, days: 7 }
    containerDeleteRetentionPolicy: { enabled: true, days: 7 }
  }
}

resource coversContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  parent: blobService
  name: 'covers'
  properties: {
    publicAccess: 'Blob' // public read for cover images
  }
}

resource syncTmpContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2024-01-01' = {
  parent: blobService
  name: 'sync-tmp'
  properties: {
    publicAccess: 'None'
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2024-01-01' = {
  parent: storage
  name: 'default'
  properties: {}
}

resource dlqQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2024-01-01' = {
  parent: queueService
  name: 'dlq'
  properties: {}
}

// Lifecycle: delete sync-tmp/* after 1 day (minimum granularity for ILM).
// Spec asked for 5 minutes; ILM only supports day-based rules, so finer
// cleanup must be performed by batch code. This rule is a safety-net.
resource storageLifecycle 'Microsoft.Storage/storageAccounts/managementPolicies@2024-01-01' = {
  parent: storage
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'delete-sync-tmp'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: [ 'blockBlob' ]
              prefixMatch: [ 'sync-tmp/' ]
            }
            actions: {
              baseBlob: {
                delete: { daysAfterModificationGreaterThan: 1 }
              }
            }
          }
        }
      ]
    }
  }
}

resource storageDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: blobService
  name: 'blob-diag'
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      { category: 'StorageRead',   enabled: true }
      { category: 'StorageWrite',  enabled: true }
      { category: 'StorageDelete', enabled: true }
    ]
    metrics: [
      { category: 'Transaction', enabled: true }
    ]
  }
}

// ----------------------------------------------------------------------------
// Outputs
// ----------------------------------------------------------------------------

@description('Azure SQL logical server name.')
output sqlServerName string = sqlServer.name

@description('Azure SQL logical server FQDN.')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Azure SQL logical server resource ID.')
output sqlServerId string = sqlServer.id

@description('Azure SQL database name.')
output sqlDatabaseName string = sqlDatabase.name

@description('Azure SQL database resource ID.')
output sqlDatabaseId string = sqlDatabase.id

@description('SQL server system-assigned managed identity principal ID.')
output sqlServerPrincipalId string = sqlServer.identity.principalId

@description('Storage account name.')
output storageAccountName string = storage.name

@description('Storage account resource ID.')
output storageAccountId string = storage.id

@description('Storage account primary blob endpoint.')
output storageBlobEndpoint string = storage.properties.primaryEndpoints.blob

@description('Storage account primary queue endpoint.')
output storageQueueEndpoint string = storage.properties.primaryEndpoints.queue
