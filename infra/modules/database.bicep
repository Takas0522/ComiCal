// PostgreSQL Flexible Server Module
// This module creates a PostgreSQL Flexible Server with environment-specific configurations
// and cost optimization for dev environment

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

@description('PostgreSQL administrator username')
@secure()
param administratorLogin string

@description('PostgreSQL administrator password')
@secure()
param administratorLoginPassword string

@description('Azure AD administrator object ID')
param aadAdminObjectId string = ''

@description('Azure AD administrator principal name (username or service principal name)')
param aadAdminPrincipalName string = ''

@description('Azure AD administrator principal type (User, Group, ServicePrincipal)')
@allowed([
  'User'
  'Group'
  'ServicePrincipal'
])
param aadAdminPrincipalType string = 'User'

@description('PostgreSQL server version')
@allowed([
  '16'
  '15'
  '14'
  '13'
  '12'
])
param postgresVersion string = '16'

@description('Tags to apply to resources')
param tags object = {}

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
var envShort = {
  dev: 'd'
  prod: 'p'
}[environmentName]

// PostgreSQL Server Naming: psql-{project}-{env}-{location}
var postgresServerName = 'psql-${projectName}-${envShort}-${locationShort}'

// Environment-specific SKU configuration for cost optimization
var skuConfig = {
  dev: {
    name: 'Standard_B2s'  // Burstable SKU for dev environment (cost-optimized)
    tier: 'Burstable'
  }
  prod: {
    name: 'Standard_B2s'  // Same as dev for small-scale production usage (cost-optimized)
    tier: 'Burstable'
  }
}

// Environment-specific storage configuration
var storageConfig = {
  dev: {
    storageSizeGB: 32  // Minimum storage for dev
  }
  prod: {
    storageSizeGB: 32  // Same as dev for small-scale production usage
  }
}

// Environment-specific backup configuration
var backupConfig = {
  dev: {
    backupRetentionDays: 7
    geoRedundantBackup: 'Disabled'
  }
  prod: {
    backupRetentionDays: 7  // Same as dev for cost optimization
    geoRedundantBackup: 'Disabled'  // Disabled for cost optimization
  }
}

// Environment-specific availability configuration
var availabilityConfig = {
  dev: {
    availabilityZone: ''  // No zone redundancy for dev
    highAvailability: {
      mode: 'Disabled'
    }
  }
  prod: {
    availabilityZone: ''  // Same as dev for cost optimization
    highAvailability: {
      mode: 'Disabled'  // Disabled for cost optimization (small-scale usage)
    }
  }
}

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: postgresServerName
  location: location
  tags: tags
  sku: {
    name: skuConfig[environmentName].name
    tier: skuConfig[environmentName].tier
  }
  properties: {
    version: postgresVersion
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorLoginPassword
    storage: {
      storageSizeGB: storageConfig[environmentName].storageSizeGB
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: backupConfig[environmentName].backupRetentionDays
      geoRedundantBackup: backupConfig[environmentName].geoRedundantBackup
    }
    highAvailability: availabilityConfig[environmentName].highAvailability
    availabilityZone: availabilityConfig[environmentName].availabilityZone
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Enabled'
      tenantId: subscription().tenantId
    }
  }
}

// Firewall rule to allow Azure services
resource firewallRuleAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgresServer
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Azure AD Administrator configuration (if provided)
resource aadAdmin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2023-03-01-preview' = if (!empty(aadAdminObjectId) && !empty(aadAdminPrincipalName)) {
  parent: postgresServer
  name: aadAdminObjectId
  properties: {
    principalType: aadAdminPrincipalType
    principalName: aadAdminPrincipalName
    tenantId: subscription().tenantId
  }
}

// Default database 'comical'
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresServer
  name: 'comical'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Outputs
output postgresServerName string = postgresServer.name
output postgresServerFqdn string = postgresServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
output postgresServerId string = postgresServer.id
output connectionStringTemplate string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=${database.name};Username={username};Password={password};SSL Mode=Require'
output skuName string = skuConfig[environmentName].name
output skuTier string = skuConfig[environmentName].tier
