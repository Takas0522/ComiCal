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

@description('Object ID of the deployment principal for Key Vault access')
param deploymentPrincipalObjectId string = ''

// Variables for naming conventions
var locationAbbreviation = {
  japaneast: 'jpe'
  japanwest: 'jpw'
  eastus: 'eus'
  westus: 'wus'
  eastasia: 'ea'
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
    enableRbacAuthorization: environmentName == 'prod' // Use RBAC for prod, Access Policies for dev
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: environmentName == 'prod' // Only enable for production
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
    // Access policies for dev environment (simpler permission model)
    accessPolicies: environmentName == 'dev' && !empty(deploymentPrincipalObjectId) ? [
      {
        objectId: deploymentPrincipalObjectId
        tenantId: subscription().tenantId
        permissions: {
          secrets: [
            'get'
            'list'
            'set'
          ]
        }
      }
    ] : []
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

// Key Vault Secrets User role definition
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

// RBAC: Grant deployment principal access to Key Vault secrets (for CI/CD)
// Note: Only enabled for production environment with proper Service Principal permissions
// Development environment uses Access Policies instead of RBAC for simplicity
resource deploymentKeyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(deploymentPrincipalObjectId) && environmentName == 'prod') {
  name: guid(keyVault.id, deploymentPrincipalObjectId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: deploymentPrincipalObjectId
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
