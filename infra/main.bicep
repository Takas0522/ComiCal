// ComiCal Infrastructure - Main Bicep Template
// This template defines the core Azure infrastructure for ComiCal application
// Following Azure Cloud Adoption Framework (CAF) naming conventions

targetScope = 'subscription'

@description('Environment name (dev, prod)')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Azure region for resources')
param location string = 'japaneast'

@description('Project name for resource naming')
param projectName string = 'comical'

@description('Tags to apply to all resources')
param tags object = {}

@description('Git tag/version for semantic versioning (optional)')
param gitTag string = ''

@description('PostgreSQL administrator username')
@secure()
param postgresAdminUsername string

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('Azure AD administrator object ID for PostgreSQL')
param postgresAadAdminObjectId string = ''

@description('Azure AD administrator principal name for PostgreSQL')
param postgresAadAdminPrincipalName string = ''

@description('Azure AD administrator principal type for PostgreSQL')
@allowed([
  'User'
  'Group'
  'ServicePrincipal'
])
param postgresAadAdminPrincipalType string = 'User'

@description('Rakuten API application ID for Batch Functions')
@secure()
param rakutenApiKey string = ''

// Variables for naming conventions following Azure CAF
var locationAbbreviation = {
  japaneast: 'jpe'
  japanwest: 'jpw'
  eastus: 'eus'
  westus: 'wus'
  eastasia: 'ea'
  southeastasia: 'sea'
}

var locationShort = locationAbbreviation[location]
var envShort = {
  dev: 'd'
  prod: 'p'
}[environmentName]

// Semantic version detection from git tag
var isSemanticVersion = !empty(gitTag) && startsWith(gitTag, 'v')
var versionTag = isSemanticVersion ? gitTag : ''

// Common tags including semantic version if available
var commonTags = union(tags, {
  environment: environmentName
  project: projectName
  managedBy: 'bicep'
  version: !empty(versionTag) ? versionTag : 'untagged'
})

// Resource Group Naming: rg-{project}-{envShort}-{location}
var resourceGroupName = 'rg-${projectName}-${envShort}-${locationShort}'

// Resource group for main application resources
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: commonTags
}

// PostgreSQL Database deployment
module database 'modules/database.bicep' = {
  name: 'database-deployment'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    administratorLogin: postgresAdminUsername
    administratorLoginPassword: postgresAdminPassword
    aadAdminObjectId: postgresAadAdminObjectId
    aadAdminPrincipalName: postgresAadAdminPrincipalName
    aadAdminPrincipalType: postgresAadAdminPrincipalType
    tags: commonTags
  }
}

// Security Module - Key Vault and secret management
module security 'modules/security.bicep' = {
  name: 'security-deployment'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    postgresServerFqdn: database.outputs.postgresServerFqdn
    databaseName: database.outputs.databaseName
    postgresAdminUsername: postgresAdminUsername
    postgresAdminPassword: postgresAdminPassword
    rakutenApiKey: rakutenApiKey
    tags: commonTags
  }
}

// Outputs
output resourceGroupName string = resourceGroup.name
output resourceGroupId string = resourceGroup.id
output location string = location
output environment string = environmentName
output semanticVersion string = versionTag
output isSemanticVersionDeployment bool = isSemanticVersion
output tags object = commonTags

// Database outputs
output postgresServerName string = database.outputs.postgresServerName
output postgresServerFqdn string = database.outputs.postgresServerFqdn
output databaseName string = database.outputs.databaseName
output postgresConnectionStringTemplate string = database.outputs.connectionStringTemplate
output postgresSku string = '${database.outputs.skuTier}/${database.outputs.skuName}'

// Security outputs
output keyVaultId string = security.outputs.keyVaultId
output keyVaultName string = security.outputs.keyVaultName
output keyVaultUri string = security.outputs.keyVaultUri
output postgresConnectionStringSecretUri string = security.outputs.postgresConnectionStringSecretUri
output rakutenApiKeySecretUri string = security.outputs.rakutenApiKeySecretUri
