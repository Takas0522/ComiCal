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

// PostgreSQL Database Module
@description('PostgreSQL administrator login name')
@secure()
param postgresAdminLogin string

@description('PostgreSQL administrator password')
@secure()
param postgresAdminPassword string

@description('Database name to create')
param databaseName string = 'comical'

@description('Allow Azure services to access PostgreSQL (set to false for stricter production security)')
param allowAzureServices bool = true

module database 'modules/database.bicep' = {
  name: 'database-deployment-${environmentName}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    locationShort: locationShort
    envShort: envShort
    tags: commonTags
    administratorLogin: postgresAdminLogin
    administratorPassword: postgresAdminPassword
    databaseName: databaseName
    allowAzureServices: allowAzureServices
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
output postgresServerName string = database.outputs.serverName
output postgresServerId string = database.outputs.serverId
output postgresServerFqdn string = database.outputs.serverFqdn
output postgresDatabaseName string = database.outputs.databaseName
output postgresConnectionString string = database.outputs.connectionStringManagedIdentity
output postgresSkuName string = database.outputs.skuName
output postgresSkuTier string = database.outputs.skuTier
