// Security Module - Key Vault, Managed Identity, and RBAC
// This module creates Key Vault with environment-specific configurations,
// enables Managed Identity for Function Apps, and sets up RBAC permissions

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

@description('PostgreSQL server FQDN for connection string')
param postgresServerFqdn string

@description('PostgreSQL database name')
param databaseName string

@description('PostgreSQL administrator username')
@secure()
param postgresAdminUsername string

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('Rakuten API application ID')
@secure()
param rakutenApiKey string = ''

@description('API Function App principal ID for RBAC')
param apiFunctionAppPrincipalId string = ''

@description('Batch Function App principal ID for RBAC')
param batchFunctionAppPrincipalId string = ''

@description('Storage Account name for RBAC')
param storageAccountName string = ''

// Variables for naming conventions
var locationAbbreviation = {
  japaneast: 'jpe'
  japanwest: 'jpw'
  eastus: 'eus'
  westus: 'wus'
  westus2: 'wu2'
  centralus: 'cus'
  eastasia: 'eas'
  southeastasia: 'sea'
}

var locationShort = locationAbbreviation[location]

// Key Vault Naming: kv-{project}-{env}-{location}
// Note: Key Vault names must be globally unique and max 24 characters
// Current: 'kv-comical-dev-jpe' = 18 chars, 'kv-comical-prod-jpe' = 19 chars
var keyVaultName = 'kv-${projectName}-${environmentName}-${locationShort}'

// SKU for Key Vault
var keyVaultSku = {
  dev: 'standard'
  prod: 'standard'
}[environmentName]

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: keyVaultSku
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true // Always use RBAC for consistency
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true // Always enabled - cannot be disabled once set
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// PostgreSQL connection string secret
resource postgresConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'PostgresConnectionString'
  properties: {
    value: 'Host=${postgresServerFqdn};Database=${databaseName};Username=${postgresAdminUsername};Password=${postgresAdminPassword};SslMode=Require'
  }
}

// Rakuten API key secret (only if provided)
resource rakutenApiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = if (!empty(rakutenApiKey)) {
  parent: keyVault
  name: 'RakutenApiKey'
  properties: {
    value: rakutenApiKey
  }
}



// RBAC Role IDs
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

// Reference to storage account for RBAC
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = if (!empty(storageAccountName)) {
  name: storageAccountName
}

// RBAC: Grant API Function App access to Key Vault secrets
resource apiFunctionAppKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiFunctionAppPrincipalId)) {
  name: guid(keyVault.id, apiFunctionAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: apiFunctionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Batch Function App access to Key Vault secrets
resource batchFunctionAppKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(batchFunctionAppPrincipalId)) {
  name: guid(keyVault.id, batchFunctionAppPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: batchFunctionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant API Function App access to Storage Blob
resource apiFunctionAppStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(apiFunctionAppPrincipalId) && !empty(storageAccountName)) {
  name: guid(storageAccount.id, apiFunctionAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: apiFunctionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Grant Batch Function App access to Storage Blob
resource batchFunctionAppStorageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(batchFunctionAppPrincipalId) && !empty(storageAccountName)) {
  name: guid(storageAccount.id, batchFunctionAppPrincipalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: batchFunctionAppPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output keyVaultId string = keyVault.id
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output postgresConnectionStringSecretUri string = postgresConnectionStringSecret.properties.secretUri
// Note: Manual URI construction for conditional secret to avoid BCP318 warning
// Accessing .properties.secretUri on a conditional resource may be null, causing deployment failure
// The URI format is standardized and safe to construct manually
#disable-next-line outputs-should-not-contain-secrets
output rakutenApiKeySecretUri string = !empty(rakutenApiKey) ? '${keyVault.properties.vaultUri}secrets/RakutenApiKey' : ''
