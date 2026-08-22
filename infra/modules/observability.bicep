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

@description('Email address for alert notifications; leave empty to disable email receiver')
param alertEmailAddress string = ''

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
    emailReceivers: !empty(alertEmailAddress) ? [
      {
        name: 'primary-email'
        emailAddress: alertEmailAddress
        useCommonAlertSchema: true
      }
    ] : []
    webhookReceivers: !empty(alertWebhookUrl) ? [
      {
        name: 'primary'
        serviceUri: alertWebhookUrl
        useCommonAlertSchema: true
      }
    ] : []
  }
}

// Fires when the Batch Function App has any failed function invocation in the
// last 15 minutes. This catches the actual failure surface (uncaught exceptions
// from FetchPageActivity / FetchOrchestrator / DailyFetchOrchestrator, etc.),
// which the previous "batch.failedItem" customMetrics query missed entirely
// because no code path emits that metric name — a prod outage on 2026-08-15
// through 2026-08-22 went silent as a result.
//
// Cost optimization: log alert rules are billed per rule per month, so deploy
// in prod only. AppRequests is a first-class table populated by the Functions
// host, so this query survives even if the app never emits any custom metric.
resource alertRule 'Microsoft.Insights/scheduledQueryRules@2022-06-15' = if (env == 'prod') {
  name: alertRuleName
  location: location
  properties: {
    displayName: 'Batch Function Failures'
    description: 'Fires when the Batch Function App records at least one failed invocation within a 15-minute window.'
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
          query: 'AppRequests | where AppRoleName == "${prefix}-${env}-jpe-func-batch" and Success == false | summarize AggregatedValue = count() by bin(TimeGenerated, 15m)'
          timeAggregation: 'Total'
          metricMeasureColumn: 'AggregatedValue'
          operator: 'GreaterThanOrEqual'
          threshold: 1
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
    // Auto-resolve once the batch recovers so the next failure raises a fresh
    // alert instead of appending to a stale one.
    autoMitigate: true
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
