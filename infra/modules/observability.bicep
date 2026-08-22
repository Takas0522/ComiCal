@description('Resource name prefix following CAF convention')
param prefix string

@description('Environment short code (dev or prod)')
param env string

@description('Azure region')
param location string

@description('Log Analytics workspace retention in days')
param logRetentionDays int

@description('Webhook URL for alert notifications (Slack or Teams); leave empty to disable')
param alertWebhookUrl string = ''

var logName = '${prefix}-${env}-jpe-log'
var appiName = '${prefix}-${env}-jpe-appi'
var actionGroupName = '${prefix}-${env}-jpe-ag'
var alertRuleName = '${prefix}-${env}-jpe-alert-batch-failed'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logName
  location: location
  properties: {
    retentionInDays: logRetentionDays
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appiName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    IngestionMode: 'LogAnalytics'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'global'
  properties: {
    groupShortName: 'cmcl'
    enabled: true
    webhookReceivers: !empty(alertWebhookUrl) ? [
      {
        name: 'primary'
        serviceUri: alertWebhookUrl
        useCommonAlertSchema: true
      }
    ] : []
  }
}

// Alert fires when the batch.failedItem custom metric total >= 5 within a 15-minute window.
// The metric is emitted by the Batch Function App via Application Insights custom metrics.
// Cost optimization: log alert rules are billed per rule per month, so deploy in prod only.
resource alertRule 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = if (env == 'prod') {
  name: alertRuleName
  location: location
  properties: {
    displayName: 'Batch Failed Items'
    description: 'Fires when batch.failedItem custom metric total reaches 5 or more in a 15-minute window'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    scopes: [
      appInsights.id
    ]
    criteria: {
      allOf: [
        {
          query: 'customMetrics | where name == "batch.failedItem" | summarize AggregatedValue = sum(value) by bin(timestamp, 15m)'
          timeAggregation: 'Total'
          metricMeasureColumn: 'AggregatedValue'
          operator: 'GreaterThanOrEqual'
          threshold: 5
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
    autoMitigate: false
  }
}

@description('Application Insights resource name (used by downstream modules to obtain connection string via existing reference)')
output appInsightsName string = appInsights.name

@description('Application Insights resource ID')
output appInsightsId string = appInsights.id

@description('Log Analytics workspace resource ID')
output logAnalyticsWorkspaceId string = logAnalytics.id

@description('Action Group resource ID')
output actionGroupId string = actionGroup.id
