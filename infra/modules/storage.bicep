// Storage Account Module
// This module creates a Storage Account with static website hosting,
// blob containers, and environment-specific configurations

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

// Storage Account Naming: st{project}{env}{location}
// Note: Storage account names must be globally unique, lowercase, and max 24 characters
// Current: 'stcomicald<location>' or 'stcomicalp<location>'
var storageAccountName = 'st${projectName}${envShort}${locationShort}'

// Environment-specific SKU configuration
var skuConfig = {
  dev: {
    name: 'Standard_LRS'  // Locally redundant storage for dev (cost-optimized)
  }
  prod: {
    name: 'Standard_LRS'  // Locally redundant storage for prod (cost-optimized for small-scale)
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: skuConfig[environmentName].name
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: true
    allowSharedKeyAccess: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Blob Service for containers
resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: [
            '*'  // Allow all origins for development; restrict in production as needed
          ]
          allowedMethods: [
            'GET'
            'HEAD'
            'OPTIONS'
          ]
          allowedHeaders: [
            '*'
          ]
          exposedHeaders: [
            '*'
          ]
          maxAgeInSeconds: 3600
        }
      ]
    }
  }
}

// Blob container for comic images
resource imagesContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'images'
  properties: {
    publicAccess: 'Blob'  // Allow public read access to blobs
  }
}

// Enable static website hosting
// Note: Static website is configured at the storage account level
// The $web container is created automatically when static website is enabled
// Unfortunately, Bicep doesn't directly support static website configuration
// This needs to be done via Azure CLI or Portal after deployment

// Outputs
output storageAccountId string = storageAccount.id
output storageAccountName string = storageAccount.name
output storageAccountPrimaryEndpoints object = storageAccount.properties.primaryEndpoints
output storageAccountBlobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output storageAccountWebEndpoint string = storageAccount.properties.primaryEndpoints.web
output imagesContainerName string = imagesContainer.name
output storageAccountConnectionStringTemplate string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey={key};EndpointSuffix=${environment().suffixes.storage}'
