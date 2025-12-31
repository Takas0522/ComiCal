// Static Web Apps Module
// This module creates Azure Static Web Apps with GitHub repository integration,
// environment-specific configurations, and disabled PR environments

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

@description('GitHub repository URL for automatic deployment')
param repositoryUrl string = 'https://github.com/Takas0522/ComiCal'

@description('GitHub repository branch')
param repositoryBranch string = 'main'

@description('GitHub repository token for authentication')
@secure()
param repositoryToken string = ''

@description('API Container App URL for backend connection')
param apiBackendUrl string

@description('SKU for Static Web Apps')
@allowed([
  'Free'
  'Standard'
])
param sku string = 'Free'

// Variables for naming conventions
var locationAbbreviation = {
  japaneast: 'jpe'
  japanwest: 'jpw'
  eastus: 'eus'
  eastus2: 'eu2'
  westus: 'wus'
  westus2: 'wu2'
  centralus: 'cus'
  eastasia: 'eas'
  southeastasia: 'sea'
}

var locationShort = locationAbbreviation[location]

// Static Web App Naming: stapp-{project}-{env}-{location}
var staticWebAppName = 'stapp-${projectName}-${environmentName}-${locationShort}'

// Environment-specific SKU configuration
var skuConfig = {
  dev: {
    name: 'Free'
    tier: 'Free'
  }
  prod: {
    name: sku
    tier: sku
  }
}

// Static Web App
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: location
  tags: tags
  sku: {
    name: skuConfig[environmentName].name
    tier: skuConfig[environmentName].tier
  }
  properties: {
    repositoryUrl: repositoryUrl
    repositoryToken: !empty(repositoryToken) ? repositoryToken : null
    branch: repositoryBranch
    buildProperties: {
      appLocation: 'src/front'
      apiLocation: ''
      outputLocation: 'dist/front/browser'
      appBuildCommand: 'npm run build'
      apiBuildCommand: ''
      skipGithubActionWorkflowGeneration: false
    }
    stagingEnvironmentPolicy: 'Disabled'  // Disable PR environments
    allowConfigFileUpdates: true
    provider: 'GitHub'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

// Backend configuration for API connection
resource staticWebAppConfig 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    API_BACKEND_URL: apiBackendUrl
  }
}

// Custom domain configuration (for future use)
// Note: Custom domains can be added after deployment through Azure Portal or CLI

// Outputs
output staticWebAppId string = staticWebApp.id
output staticWebAppName string = staticWebApp.name
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output staticWebAppApiKey string = staticWebApp.listSecrets().properties.apiKey
output staticWebAppRepositoryUrl string = staticWebApp.properties.repositoryUrl
output staticWebAppBranch string = staticWebApp.properties.branch
output stagingEnvironmentPolicy string = staticWebApp.properties.stagingEnvironmentPolicy
