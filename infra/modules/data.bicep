@description('Resource name prefix following CAF convention')
param prefix string

@description('Environment short code (dev or prod)')
param env string

@description('Azure region')
param location string

@description('Number of SQL vCores for the Serverless GP_S_Gen5 tier (1 for dev, 2 for prod)')
param sqlVCores int = 1

@description('SQL Server administrator login name')
param sqlAdminLogin string = 'sqladmin'

@secure()
@description('SQL Server administrator password')
param sqlAdminPassword string

var sqlServerName = '${prefix}-${env}-jpe-sql'
var sqlDatabaseName = '${prefix}-${env}-jpe-sqldb'
// Storage account names must be lowercase alphanumeric, no hyphens, max 24 chars.
// cmcldevjpest = 12 chars, cmclprodjpest = 13 chars — both within limit.
// Note: storage account names must be globally unique across Azure.
var storageAccountName = '${prefix}${env}jpest'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    // Allow Azure services to connect (required for Functions → SQL without VNet)
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure-internal IPs (0.0.0.0/0.0.0.0 is the Azure convention for "Allow Azure Services")
resource sqlFirewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: sqlVCores
  }
  properties: {
    collation: 'Japanese_CI_AS'
    // Auto-pause after 60 minutes of inactivity (cost optimisation)
    autoPauseDelay: 60
    // Minimum vCores when active; 1 keeps costs low while ensuring predictable cold-start
    minCapacity: json('1.0')
    // LRS for cost minimisation (DR not required per spec)
    requestedBackupStorageRedundancy: 'Local'
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Public read access for direct cover image serving (no CDN in MVP)
resource containerCovers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'covers'
  properties: {
    publicAccess: 'Blob'
  }
}

// Private container for in-flight batch sync artifacts
resource containerSyncTmp 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'sync-tmp'
  properties: {
    publicAccess: 'None'
  }
}

// FlexConsumption deployment package containers (one per Function App).
// The Function App's Managed Identity uploads its zip here on each deployment.
resource containerAppPackageApi 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'app-package-api'
  properties: {
    publicAccess: 'None'
  }
}

resource containerAppPackageBatch 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'app-package-batch'
  properties: {
    publicAccess: 'None'
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource failedItemsDlq 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: 'failed-items-dlq'
}

// Lifecycle policy: auto-delete sync-tmp blobs after 1 day as a safety net.
// Azure Blob lifecycle management supports a minimum of 1 day — the 5-minute design TTL
// is enforced by application logic (batch deletes blobs after use); this rule is a fallback.
resource managementPolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: storageAccount
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
              blobTypes: [
                'blockBlob'
              ]
              prefixMatch: [
                'sync-tmp/'
              ]
            }
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: 1
                }
              }
            }
          }
        }
      ]
    }
  }
}

@description('SQL Server fully-qualified domain name')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('SQL Database name')
output sqlDatabaseName string = sqlDatabaseName

@description('Storage account name')
output storageAccountName string = storageAccount.name

@description('Storage account resource ID (used by app module to retrieve access keys for Key Vault secret)')
output storageAccountId string = storageAccount.id
