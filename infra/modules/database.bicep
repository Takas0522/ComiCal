// ComiCal PostgreSQL Flexible Server Module
// This module creates a PostgreSQL Flexible Server with Managed Identity and Azure AD authentication
// Supports environment-specific SKU configuration for cost optimization

@description('Environment name (dev, prod)')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Azure region for resources')
param location string

@description('Project name for resource naming')
param projectName string

@description('Location abbreviation for naming')
param locationShort string

@description('Environment abbreviation for naming')
param envShort string

@description('Tags to apply to all resources')
param tags object = {}

@description('PostgreSQL administrator login name')
@secure()
param administratorLogin string

@description('PostgreSQL administrator password')
@secure()
param administratorPassword string

@description('Database name to create')
param databaseName string = 'comical'

@description('Managed Identity principal ID for database access')
param managedIdentityPrincipalId string = ''

@description('Allow Azure services to access the server')
param allowAzureServices bool = true

// SKU configuration based on environment
var skuConfig = {
  dev: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  prod: {
    name: 'Standard_D2s_v3'
    tier: 'GeneralPurpose'
  }
}

// Storage configuration based on environment
var storageConfig = {
  dev: {
    storageSizeGB: 32
  }
  prod: {
    storageSizeGB: 128
  }
}

// Backup configuration
var backupConfig = {
  dev: {
    backupRetentionDays: 7
    geoRedundantBackup: 'Disabled'
  }
  prod: {
    backupRetentionDays: 30
    geoRedundantBackup: 'Enabled'
  }
}

// High availability configuration (only for prod)
var highAvailabilityConfig = {
  dev: {
    mode: 'Disabled'
  }
  prod: {
    mode: 'ZoneRedundant'
  }
}

// PostgreSQL Server Naming: psql-{project}-{env}-{location}
var serverName = 'psql-${projectName}-${envShort}-${locationShort}'

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: skuConfig[environmentName].name
    tier: skuConfig[environmentName].tier
  }
  properties: {
    version: '15'
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    storage: {
      storageSizeGB: storageConfig[environmentName].storageSizeGB
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: backupConfig[environmentName].backupRetentionDays
      geoRedundantBackup: backupConfig[environmentName].geoRedundantBackup
    }
    highAvailability: {
      mode: highAvailabilityConfig[environmentName].mode
    }
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Enabled'
      tenantId: subscription().tenantId
    }
  }
}

// Create database
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Firewall rule to allow Azure services
resource firewallRuleAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = if (allowAzureServices) {
  parent: postgresServer
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Azure AD Administrator configuration (if Managed Identity is provided)
resource aadAdministrator 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2023-03-01-preview' = if (!empty(managedIdentityPrincipalId)) {
  parent: postgresServer
  name: managedIdentityPrincipalId
  properties: {
    principalName: 'comical-functions-${environmentName}'
    principalType: 'ServicePrincipal'
    tenantId: subscription().tenantId
  }
}

// Outputs
output serverName string = postgresServer.name
output serverId string = postgresServer.id
output serverFqdn string = postgresServer.properties.fullyQualifiedDomainName
output databaseName string = database.name

// Connection string for Functions (with Managed Identity) - excludes password
@description('Connection string for Managed Identity authentication (no password)')
output connectionStringManagedIdentity string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=${databaseName}'

output skuName string = skuConfig[environmentName].name
output skuTier string = skuConfig[environmentName].tier
