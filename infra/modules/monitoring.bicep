// Monitoring Module
// This module creates Application Insights and Alert Rules for monitoring and alerting
// Includes Function error alerts, PostgreSQL CPU alerts, Application Insights exception alerts,
// Container Jobs batch monitoring (failure, delay, long-running), API Key security alerts,
// and Batch Progress Dashboard (Workbook) for job visualization

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

// Action Group short name generation
// Azure requires: max 12 characters, alphanumeric only, cannot start with number
// Strategy: remove 'ag-' prefix and hyphens, take first 12 chars
var actionGroupShortName = take(replace(replace(actionGroupName, 'ag-', ''), '-', ''), 12)

// Alert Rule Naming: alert-{project}-{resource}-{metric}-{env}
var functionErrorAlertName = 'alert-${projectName}-func-5xx-${environmentName}'
var postgresAlertName = 'alert-${projectName}-psql-cpu-${environmentName}'
var appInsightsExceptionAlertName = 'alert-${projectName}-appi-exceptions-${environmentName}'
var jobFailureAlertName = 'alert-${projectName}-job-failure-${environmentName}'
var jobDelayAlertName = 'alert-${projectName}-job-delay-${environmentName}'
var jobLongRunningAlertName = 'alert-${projectName}-job-longrun-${environmentName}'
var apiKeyUnauthorizedAlertName = 'alert-${projectName}-apikey-unauth-${environmentName}'
var workbookName = 'workbook-${projectName}-batch-dashboard-${environmentName}'

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
    groupShortName: actionGroupShortName
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
                '500'
                '501'
                '502'
                '503'
                '504'
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

// Scheduled Query Alert Rule: Batch Job Failure Detection
resource jobFailureAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (length(alertEmailAddresses) > 0) {
  name: jobFailureAlertName
  location: location
  tags: tags
  properties: {
    displayName: 'Batch Job Failure Alert'
    description: 'Alert when batch jobs (data registration or image download) fail or encounter dependency failures'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT5M'
    scopes: [
      appInsights.id
    ]
    windowSize: 'PT15M'
    criteria: {
      allOf: [
        {
          query: '''
            traces
            | where message has "Job failed" or message has "dependency failure" or message has "manual intervention required"
            | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
            | summarize Count = count() by JobType = tostring(customDimensions.JobType)
            '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Scheduled Query Alert Rule: Job Delay Detection (3+ retries)
resource jobDelayAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (length(alertEmailAddresses) > 0) {
  name: jobDelayAlertName
  location: location
  tags: tags
  properties: {
    displayName: 'Batch Job Delay Alert'
    description: 'Alert when batch jobs experience 3 or more delays/retries'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT5M'
    scopes: [
      appInsights.id
    ]
    windowSize: 'PT30M'
    criteria: {
      allOf: [
        {
          query: '''
            traces
            | where message has "retry" or message has "delay"
            | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
            | summarize RetryCount = count() by JobType = tostring(customDimensions.JobType), JobId = tostring(customDimensions.JobId)
            | where RetryCount >= 3
            '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Scheduled Query Alert Rule: Long-Running Job Detection
resource jobLongRunningAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (length(alertEmailAddresses) > 0) {
  name: jobLongRunningAlertName
  location: location
  tags: tags
  properties: {
    displayName: 'Long-Running Job Alert'
    description: 'Alert when batch jobs run for more than 30 minutes (potential timeout)'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT15M'
    scopes: [
      appInsights.id
    ]
    windowSize: 'PT30M'
    criteria: {
      allOf: [
        {
          query: '''
            traces
            | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
            | where message has "Job started"
            | extend StartTime = timestamp
            | join kind=leftouter (
                traces
                | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
                | where message has "Job completed" or message has "Job failed"
                | extend EndTime = timestamp
            ) on $left.customDimensions.JobId == $right.customDimensions.JobId
            | where isnull(EndTime) or (EndTime - StartTime) > 30m
            | summarize Count = count() by JobType = tostring(customDimensions.JobType)
            '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Scheduled Query Alert Rule: API Key Unauthorized Access
resource apiKeyUnauthorizedAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = if (length(alertEmailAddresses) > 0) {
  name: apiKeyUnauthorizedAlertName
  location: location
  tags: tags
  properties: {
    displayName: 'API Key Unauthorized Access Alert'
    description: 'Alert when API key unauthorized access or invalid API key errors are detected'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT5M'
    scopes: [
      appInsights.id
    ]
    windowSize: 'PT15M'
    criteria: {
      allOf: [
        {
          query: '''
            union traces, exceptions
            | where message has "unauthorized" or message has "401" or message has "403" or message has "invalid API key"
            | where customDimensions.ApiKeySource == "Rakuten" or message has "API key"
            | summarize Count = count() by ResultCode = tostring(customDimensions.ResultCode)
            '''
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 3
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Batch Progress Dashboard Workbook
resource batchDashboard 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: guid(workbookName)
  location: location
  tags: tags
  kind: 'shared'
  properties: {
    displayName: 'Batch Job Progress Dashboard - ${environmentName}'
    serializedData: string({
      version: 'Notebook/1.0'
      items: [
        {
          type: 1
          content: {
            json: '## Batch Job Progress Dashboard\n\nMonitoring dashboard for Container Jobs batch processing (Data Registration & Image Download)'
          }
        }
        {
          type: 3
          content: {
            version: 'KqlItem/1.0'
            query: '''
              traces
              | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
              | summarize 
                  TotalJobs = dcount(tostring(customDimensions.JobId)),
                  SuccessfulJobs = dcountif(tostring(customDimensions.JobId), message has "Job completed"),
                  FailedJobs = dcountif(tostring(customDimensions.JobId), message has "Job failed")
              | extend SuccessRate = round(100.0 * SuccessfulJobs / TotalJobs, 2)
              | project TotalJobs, SuccessfulJobs, FailedJobs, SuccessRate
              '''
            size: 3
            title: 'Job Summary (Last 24 hours)'
            timeContext: {
              durationMs: 86400000
            }
            queryType: 0
            resourceType: 'microsoft.insights/components'
            visualization: 'tiles'
            tileSettings: {
              showBorder: false
            }
          }
        }
        {
          type: 3
          content: {
            version: 'KqlItem/1.0'
            query: '''
              traces
              | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
              | where isnotempty(customDimensions.ProgressRate)
              | extend ProgressRate = todouble(customDimensions.ProgressRate)
              | summarize avg(ProgressRate) by bin(timestamp, 5m), JobType = tostring(customDimensions.JobType)
              | render timechart
              '''
            size: 0
            title: 'Job Progress Rate (%)'
            timeContext: {
              durationMs: 86400000
            }
            queryType: 0
            resourceType: 'microsoft.insights/components'
            visualization: 'timechart'
          }
        }
        {
          type: 3
          content: {
            version: 'KqlItem/1.0'
            query: '''
              traces
              | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
              | where isnotempty(customDimensions.ProcessingTimeMs)
              | extend ProcessingTime = todouble(customDimensions.ProcessingTimeMs) / 1000
              | summarize avg(ProcessingTime), percentile(ProcessingTime, 95) by bin(timestamp, 5m), JobType = tostring(customDimensions.JobType)
              | render timechart
              '''
            size: 0
            title: 'Job Processing Time (seconds)'
            timeContext: {
              durationMs: 86400000
            }
            queryType: 0
            resourceType: 'microsoft.insights/components'
            visualization: 'timechart'
          }
        }
        {
          type: 3
          content: {
            version: 'KqlItem/1.0'
            query: '''
              traces
              | where message has "Job failed" or message has "error"
              | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
              | summarize Count = count() by JobType = tostring(customDimensions.JobType), ErrorMessage = tostring(customDimensions.ErrorMessage)
              | order by Count desc
              | take 10
              '''
            size: 0
            title: 'Top 10 Error Messages'
            timeContext: {
              durationMs: 86400000
            }
            queryType: 0
            resourceType: 'microsoft.insights/components'
            visualization: 'table'
          }
        }
        {
          type: 3
          content: {
            version: 'KqlItem/1.0'
            query: '''
              traces
              | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
              | where message has "retry" or message has "delay"
              | summarize RetryCount = count() by JobType = tostring(customDimensions.JobType), JobId = tostring(customDimensions.JobId)
              | order by RetryCount desc
              | take 10
              '''
            size: 0
            title: 'Jobs with Most Retries'
            timeContext: {
              durationMs: 86400000
            }
            queryType: 0
            resourceType: 'microsoft.insights/components'
            visualization: 'table'
          }
        }
      ]
    })
    category: 'workbook'
    sourceId: appInsights.id
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
output batchDashboardId string = batchDashboard.id
output batchDashboardName string = batchDashboard.properties.displayName
