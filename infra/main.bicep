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

@description('Skip RBAC assignments (for Service Principal permission issues)')
param skipRbacAssignments bool = false

@description('GitHub repository token for Static Web Apps auto-linking')
@secure()
param githubToken string = ''

@description('GitHub repository URL for Static Web Apps')
param repositoryUrl string = 'https://github.com/Takas0522/ComiCal'

@description('Current UTC timestamp for unique deployment naming') 
param timestamp string = utcNow()

@description('GitHub repository branch for Static Web Apps')
param repositoryBranch string = 'main'

@description('Alert notification email addresses for monitoring')
param alertEmailAddresses array = []

// Variables for naming conventions following Azure CAF
var locationAbbreviation = {
  japaneast: 'jpe'
  japanwest: 'jpw'
  eastus: 'eus'
  eastus2: 'eu2'  // VMクォータ制限回避のため追加
  westus: 'wus'
  westus2: 'wu2'
  centralus: 'cus'
  eastasia: 'eas'
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

// Resource Group Naming: rg-{project}-{envShort}-{location}
var resourceGroupName = 'rg-${projectName}-${envShort}-${locationShort}'

// Unique deployment suffix to avoid deployment name conflicts
var deploymentSuffix = uniqueString(subscription().subscriptionId, resourceGroupName, timestamp)

// Common tags including semantic version if available
var commonTags = union(tags, {
  environment: environmentName
  project: projectName
  managedBy: 'bicep'
  version: !empty(versionTag) ? versionTag : 'untagged'
})

// Resource group for main application resources
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: commonTags
}

// PostgreSQL Database deployment
module database 'modules/database.bicep' = {
  name: 'database-deployment-${deploymentSuffix}'
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
  name: 'security-deployment-${deploymentSuffix}'
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

// Storage Account deployment
module storage 'modules/storage.bicep' = {
  name: 'storage-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    tags: commonTags
  }
}

// Monitoring Module - Application Insights (deployed first so Container Apps can use it)
module monitoringBase 'modules/monitoring.bicep' = {
  name: 'monitoring-base-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    alertEmailAddresses: [] // Don't create alerts yet
    apiContainerAppId: '' 
    batchContainerAppId: '' 
    postgresServerId: ''
    tags: commonTags
  }
}

// Container Apps deployment for Batch processing
// Note: Web API is deployed as Static Web Apps Managed Functions
module containerApps 'modules/container-apps.bicep' = {
  name: 'container-apps-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    storageAccountName: storage.outputs.storageAccountName
    appInsightsConnectionString: monitoringBase.outputs.appInsightsConnectionString
    postgresConnectionStringSecretUri: security.outputs.postgresConnectionStringSecretUri
    rakutenApiKeySecretUri: security.outputs.rakutenApiKeySecretUri
    tags: commonTags
  }
}

// Container Jobs Module - Scheduled batch processing and manual execution
module containerJobs 'modules/container-jobs.bicep' = {
  name: 'container-jobs-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    storageAccountName: storage.outputs.storageAccountName
    appInsightsConnectionString: monitoringBase.outputs.appInsightsConnectionString
    postgresConnectionStringSecretUri: security.outputs.postgresConnectionStringSecretUri
    rakutenApiKeySecretUri: security.outputs.rakutenApiKeySecretUri
    existingContainerAppsEnvironmentId: containerApps.outputs.containerAppsEnvironmentId
    tags: commonTags
  }
}

// Update Security Module with Container App RBAC (RBAC権限がある場合のみ)
module securityRbac 'modules/security.bicep' = if (!skipRbacAssignments) {
  name: 'security-rbac-deployment-${deploymentSuffix}'
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
    apiFunctionAppPrincipalId: '' // API is deployed as Static Web Apps Managed Functions
    batchFunctionAppPrincipalId: containerApps.outputs.batchContainerAppPrincipalId
    dataRegistrationJobPrincipalId: containerJobs.outputs.dataRegistrationJobPrincipalId
    imageDownloadJobPrincipalId: containerJobs.outputs.imageDownloadJobPrincipalId
    manualBatchContainerAppPrincipalId: containerJobs.outputs.manualBatchContainerAppPrincipalId
    storageAccountName: storage.outputs.storageAccountName
    tags: commonTags
  }
}

// Monitoring Alerts Module - Deploy alert rules after Container Apps are ready
module monitoringAlerts 'modules/monitoring.bicep' = {
  name: 'monitoring-alerts-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    alertEmailAddresses: alertEmailAddresses
    apiContainerAppId: '' // API is deployed as Static Web Apps Managed Functions
    batchContainerAppId: containerApps.outputs.batchContainerAppId
    postgresServerId: database.outputs.postgresServerId
    tags: commonTags
  }
}

// Cost Optimization Module - Night shutdown for dev environment (RBAC権限がある場合のみ)
module costOptimization 'modules/cost-optimization.bicep' = if (!skipRbacAssignments) {
  name: 'cost-optimization-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    apiFunctionAppId: '' // API is deployed as Static Web Apps Managed Functions
    batchFunctionAppId: containerApps.outputs.batchContainerAppId
    tags: commonTags
  }
}

// CDN Module - Production only
module cdn 'modules/cdn.bicep' = {
  name: 'cdn-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    storageWebEndpoint: storage.outputs.storageAccountWebEndpoint
    tags: commonTags
  }
}

// Static Web Apps Module - Environment-specific frontend hosting
module staticWebApp 'modules/staticwebapp.bicep' = {
  name: 'staticwebapp-deployment-${deploymentSuffix}'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    repositoryUrl: repositoryUrl
    repositoryBranch: repositoryBranch
    repositoryToken: githubToken
    apiBackendUrl: '' // Using Static Web Apps integrated Managed Functions
    sku: environmentName == 'prod' ? 'Standard' : 'Free'
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

// Database outputs (essential)
output postgresServerName string = database.outputs.postgresServerName
output postgresServerFqdn string = database.outputs.postgresServerFqdn
output databaseName string = database.outputs.databaseName

// Security outputs (essential)
output keyVaultId string = security.outputs.keyVaultId
output keyVaultName string = security.outputs.keyVaultName
output keyVaultUri string = security.outputs.keyVaultUri

// Storage outputs (essential)
output storageAccountId string = storage.outputs.storageAccountId
output storageAccountName string = storage.outputs.storageAccountName
output storageAccountBlobEndpoint string = storage.outputs.storageAccountBlobEndpoint

// Batch Container App outputs (essential for CI)
output batchContainerAppId string = containerApps.outputs.batchContainerAppId
output batchContainerAppName string = containerApps.outputs.batchContainerAppName

// Legacy compatibility outputs for CI (API is now Static Web Apps Managed Functions)
// These outputs provide placeholder values for backward compatibility
output apiFunctionAppId string = staticWebApp.outputs.staticWebAppId
output apiFunctionAppName string = staticWebApp.outputs.staticWebAppName
output apiFunctionAppHostname string = staticWebApp.outputs.staticWebAppDefaultHostname
output batchFunctionAppId string = containerApps.outputs.batchContainerAppId
output batchFunctionAppName string = containerApps.outputs.batchContainerAppName

// Monitoring outputs (essential for CI)
output appInsightsId string = monitoringBase.outputs.appInsightsId
output appInsightsName string = monitoringBase.outputs.appInsightsName
output appInsightsConnectionString string = monitoringBase.outputs.appInsightsConnectionString
output logAnalyticsWorkspaceId string = monitoringBase.outputs.logAnalyticsWorkspaceId
output actionGroupName string = monitoringAlerts.outputs.actionGroupName
output alertsEnabled bool = monitoringAlerts.outputs.alertsEnabled

// Static Web Apps outputs (essential)
output staticWebAppId string = staticWebApp.outputs.staticWebAppId
output staticWebAppName string = staticWebApp.outputs.staticWebAppName
output staticWebAppDefaultHostname string = staticWebApp.outputs.staticWebAppDefaultHostname

// CDN outputs (essential)
output cdnEnabled bool = cdn.outputs.cdnEnabled
output cdnEndpointHostname string = cdn.outputs.cdnEndpointHostname
