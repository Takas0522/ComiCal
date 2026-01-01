// Monitoring Module
// This module creates Application Insights and Alert Rules for monitoring and alerting
// Includes Function error alerts, PostgreSQL CPU alerts, and Application Insights exception alerts

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

@description('Alert notification email addresses')
param alertEmailAddresses array = []

@description('API Container App resource ID for alert rules')
param apiContainerAppId string

@description('Batch Container App resource ID for alert rules')
param batchContainerAppId string

@description('PostgreSQL Server resource ID for alert rules')
param postgresServerId string

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

// Application Insights Naming: appi-{project}-{env}-{location}
var appInsightsName = 'appi-${projectName}-${environmentName}-${locationShort}'

// Log Analytics Workspace Naming: law-{project}-{env}-{location}
var logAnalyticsWorkspaceName = 'law-${projectName}-${environmentName}-${locationShort}'

// Action Group Naming: ag-{project}-alerts
var actionGroupName = 'ag-${projectName}-alerts'

// Alert Rule Naming: alert-{project}-{resource}-{metric}-{env}
var functionErrorAlertName = 'alert-${projectName}-func-5xx-${environmentName}'
var postgresAlertName = 'alert-${projectName}-psql-cpu-${environmentName}'
var appInsightsExceptionAlertName = 'alert-${projectName}-appi-exceptions-${environmentName}'

// Log Analytics Workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environmentName == 'dev' ? 30 : 90
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    RetentionInDays: environmentName == 'dev' ? 30 : 90
    WorkspaceResourceId: logAnalyticsWorkspace.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Action Group for Email Notifications
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = if (length(alertEmailAddresses) > 0) {
  name: actionGroupName
  location: 'global'
  tags: tags
  properties: {
    groupShortName: take(replace(replace(actionGroupName, '-', ''), 'ag', ''), 12)
    enabled: true
    emailReceivers: [for (email, i) in alertEmailAddresses: {
      name: 'email-${i}'
      emailAddress: email
      useCommonAlertSchema: true
    }]
  }
}

// Alert Rule: Function HTTP 5xx errors (> 5 occurrences)
resource functionErrorAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (length(alertEmailAddresses) > 0 && !empty(apiContainerAppId)) {
  name: functionErrorAlertName
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Function Apps return more than 5 HTTP 5xx errors'
    severity: 2
    enabled: true
    scopes: [
      apiContainerAppId
      batchContainerAppId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'Http5xxErrors'
          metricName: 'Requests'
          metricNamespace: 'Microsoft.App/containerApps'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Count'
          dimensions: [
            {
              name: 'StatusCode'
              operator: 'Include'
              values: [
                '5*'
              ]
            }
          ]
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Alert Rule: PostgreSQL CPU Usage (> 80%)
resource postgresAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (length(alertEmailAddresses) > 0 && !empty(postgresServerId)) {
  name: postgresAlertName
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when PostgreSQL CPU usage exceeds 80%'
    severity: 2
    enabled: true
    scopes: [
      postgresServerId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'CpuPercent'
          metricName: 'cpu_percent'
          metricNamespace: 'Microsoft.DBforPostgreSQL/flexibleServers'
          operator: 'GreaterThan'
          threshold: 80
          timeAggregation: 'Average'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Alert Rule: Application Insights Exceptions
resource appInsightsExceptionAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (length(alertEmailAddresses) > 0) {
  name: appInsightsExceptionAlertName
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Application Insights detects exceptions'
    severity: 3
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          criterionType: 'StaticThresholdCriterion'
          name: 'ExceptionCount'
          metricName: 'exceptions/count'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Count'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Outputs
output appInsightsId string = appInsights.id
output appInsightsName string = appInsights.name
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output logAnalyticsWorkspaceName string = logAnalyticsWorkspace.name
output actionGroupId string = length(alertEmailAddresses) > 0 ? actionGroup.id : ''
output actionGroupName string = length(alertEmailAddresses) > 0 ? actionGroup.name : ''
output alertsEnabled bool = length(alertEmailAddresses) > 0
