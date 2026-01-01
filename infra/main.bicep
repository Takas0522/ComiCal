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

// Storage Account deployment
module storage 'modules/storage.bicep' = {
  name: 'storage-deployment'
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
  name: 'monitoring-base-deployment'
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

// Container Apps deployment (VMクォータ制限のためFunction Appsから変更)
module containerApps 'modules/container-apps.bicep' = {
  name: 'container-apps-deployment'
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

// Update Security Module with Container App RBAC (RBAC権限がある場合のみ)
module securityRbac 'modules/security.bicep' = if (!skipRbacAssignments) {
  name: 'security-rbac-deployment'
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
    apiFunctionAppPrincipalId: containerApps.outputs.apiContainerAppPrincipalId
    batchFunctionAppPrincipalId: containerApps.outputs.batchContainerAppPrincipalId
    storageAccountName: storage.outputs.storageAccountName
    tags: commonTags
  }
}

// Monitoring Alerts Module - Deploy alert rules after Container Apps are ready
module monitoringAlerts 'modules/monitoring.bicep' = {
  name: 'monitoring-alerts-deployment'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    alertEmailAddresses: alertEmailAddresses
    apiContainerAppId: containerApps.outputs.apiContainerAppId
    batchContainerAppId: containerApps.outputs.batchContainerAppId
    postgresServerId: database.outputs.postgresServerId
    tags: commonTags
  }
}

// Cost Optimization Module - Night shutdown for dev environment (RBAC権限がある場合のみ)
module costOptimization 'modules/cost-optimization.bicep' = if (!skipRbacAssignments) {
  name: 'cost-optimization-deployment'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    apiFunctionAppId: containerApps.outputs.apiContainerAppId
    batchFunctionAppId: containerApps.outputs.batchContainerAppId
    tags: commonTags
  }
}

// CDN Module - Production only
module cdn 'modules/cdn.bicep' = {
  name: 'cdn-deployment'
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
  name: 'staticwebapp-deployment'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    repositoryUrl: repositoryUrl
    repositoryBranch: repositoryBranch
    repositoryToken: githubToken
    apiBackendUrl: containerApps.outputs.apiContainerAppUrl
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

// Storage outputs
output storageAccountId string = storage.outputs.storageAccountId
output storageAccountName string = storage.outputs.storageAccountName
output storageAccountBlobEndpoint string = storage.outputs.storageAccountBlobEndpoint
output storageAccountWebEndpoint string = storage.outputs.storageAccountWebEndpoint
output imagesContainerName string = storage.outputs.imagesContainerName

// Functions outputs
output appServicePlanId string = ''
output appServicePlanName string = ''
output appServicePlanSku string = ''
output apiFunctionAppId string = containerApps.outputs.apiContainerAppId
output apiFunctionAppName string = containerApps.outputs.apiContainerAppName
output apiFunctionAppHostname string = containerApps.outputs.apiContainerAppUrl
output batchFunctionAppId string = containerApps.outputs.batchContainerAppId
output batchFunctionAppName string = containerApps.outputs.batchContainerAppName
output batchFunctionAppHostname string = ''
output appInsightsConnectionString string = monitoringBase.outputs.appInsightsConnectionString

// Monitoring outputs
output appInsightsId string = monitoringBase.outputs.appInsightsId
output appInsightsName string = monitoringBase.outputs.appInsightsName
output appInsightsInstrumentationKey string = monitoringBase.outputs.appInsightsInstrumentationKey
output logAnalyticsWorkspaceId string = monitoringBase.outputs.logAnalyticsWorkspaceId
output logAnalyticsWorkspaceName string = monitoringBase.outputs.logAnalyticsWorkspaceName
output actionGroupId string = monitoringAlerts.outputs.actionGroupId
output actionGroupName string = monitoringAlerts.outputs.actionGroupName
output alertsEnabled bool = monitoringAlerts.outputs.alertsEnabled

// Cost Optimization outputs (RBAC権限がある場合のみ)
output nightShutdownEnabled bool = skipRbacAssignments ? false : costOptimization!.outputs.nightShutdownEnabled
output stopLogicAppName string = skipRbacAssignments ? '' : costOptimization!.outputs.stopLogicAppName
output startLogicAppName string = skipRbacAssignments ? '' : costOptimization!.outputs.startLogicAppName

// CDN outputs
output cdnEnabled bool = cdn.outputs.cdnEnabled
output cdnEndpointHostname string = cdn.outputs.cdnEndpointHostname
output cdnEndpointName string = cdn.outputs.cdnEndpointName

// Static Web Apps outputs
output staticWebAppId string = staticWebApp.outputs.staticWebAppId
output staticWebAppName string = staticWebApp.outputs.staticWebAppName
output staticWebAppDefaultHostname string = staticWebApp.outputs.staticWebAppDefaultHostname
output staticWebAppRepositoryUrl string = staticWebApp.outputs.staticWebAppRepositoryUrl
output staticWebAppBranch string = staticWebApp.outputs.staticWebAppBranch
output stagingEnvironmentPolicy string = staticWebApp.outputs.stagingEnvironmentPolicy
