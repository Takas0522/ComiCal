// CDN Module
// This module creates Azure CDN for production environment only
// Integrates with Storage Account static website hosting

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

@description('Storage Account primary web endpoint')
param storageWebEndpoint string

// Only deploy CDN for prod environment
var deployCdn = environmentName == 'prod'

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

// CDN Profile Naming: cdn-{project}-{env}
var cdnProfileName = 'cdn-${projectName}-${environmentName}'

// CDN Endpoint Naming: cdn-{project}-{env}-{location}
var cdnEndpointName = 'cdn-${projectName}-${environmentName}-${locationShort}'

// Extract origin hostname from storage web endpoint
// Storage web endpoint format: https://{accountname}.z11.web.core.windows.net/
var originHostname = replace(replace(storageWebEndpoint, 'https://', ''), '/', '')

// CDN Profile (Standard Microsoft)
resource cdnProfile 'Microsoft.Cdn/profiles@2023-05-01' = if (deployCdn) {
  name: cdnProfileName
  location: 'Global'  // CDN is a global service
  tags: tags
  sku: {
    name: 'Standard_Microsoft'  // Standard Microsoft SKU for cost optimization
  }
  properties: {}
}

// CDN Endpoint
resource cdnEndpoint 'Microsoft.Cdn/profiles/endpoints@2023-05-01' = if (deployCdn) {
  parent: cdnProfile
  name: cdnEndpointName
  location: 'Global'
  tags: tags
  properties: {
    originHostHeader: originHostname
    isHttpAllowed: true
    isHttpsAllowed: true
    queryStringCachingBehavior: 'IgnoreQueryString'
    contentTypesToCompress: [
      'application/eot'
      'application/font'
      'application/font-sfnt'
      'application/javascript'
      'application/json'
      'application/opentype'
      'application/otf'
      'application/pkcs7-mime'
      'application/truetype'
      'application/ttf'
      'application/vnd.ms-fontobject'
      'application/xhtml+xml'
      'application/xml'
      'application/xml+rss'
      'application/x-font-opentype'
      'application/x-font-truetype'
      'application/x-font-ttf'
      'application/x-httpd-cgi'
      'application/x-javascript'
      'application/x-mpegurl'
      'application/x-opentype'
      'application/x-otf'
      'application/x-perl'
      'application/x-ttf'
      'font/eot'
      'font/ttf'
      'font/otf'
      'font/opentype'
      'image/svg+xml'
      'text/css'
      'text/csv'
      'text/html'
      'text/javascript'
      'text/js'
      'text/plain'
      'text/richtext'
      'text/tab-separated-values'
      'text/xml'
      'text/x-script'
      'text/x-component'
      'text/x-java-source'
    ]
    isCompressionEnabled: true
    origins: [
      {
        name: 'storage-origin'
        properties: {
          hostName: originHostname
          httpPort: 80
          httpsPort: 443
          originHostHeader: originHostname
        }
      }
    ]
  }
}

// Outputs
output cdnProfileId string = deployCdn ? cdnProfile.id : ''
output cdnProfileName string = deployCdn ? cdnProfile.name : ''
output cdnEndpointId string = deployCdn ? cdnEndpoint.id : ''
output cdnEndpointName string = deployCdn ? cdnEndpoint.name : ''
output cdnEndpointHostname string = deployCdn ? cdnEndpoint.properties.hostName : ''
output cdnEnabled bool = deployCdn
