@description('Resource name prefix following CAF convention')
param prefix string

@description('Environment short code (dev or prod)')
param env string

@description('Azure region')
param location string

@description('Log Analytics workspace retention in days')
param logRetentionDays int

@description('Daily ingestion cap in GB (string, e.g. "0.2"); ingestion stops for the day once reached')
param logDailyQuotaGb string = '0.2'

@description('Webhook URL for alert notifications (Slack or Teams); leave empty to disable')
param alertWebhookUrl string = ''

@description('Email address for alert notifications; leave empty to disable email receiver')
param alertEmailAddress string = ''

var logName = '${prefix}-${env}-jpe-log'
var appiName = '${prefix}-${env}-jpe-appi'
var actionGroupName = '${prefix}-${env}-jpe-ag'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logName
  location: location
  properties: {
    retentionInDays: logRetentionDays
    sku: {
      name: 'PerGB2018'
    }
    // Cost guard-rail: a personal app ingests well under 1 GB/month.
    workspaceCapping: {
      dailyQuotaGb: json(logDailyQuotaGb)
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

// NOTE: the former "Batch Function Failures" log alert was removed together with the batch
// Function App (the batch now runs locally and reports failures itself). Log alert rules are
// billed per rule per month, so none are deployed. The action group is kept for budget alerts.

@description('Application Insights resource name (used by downstream modules to obtain connection string via existing reference)')
output appInsightsName string = appInsights.name

@description('Application Insights resource ID')
output appInsightsId string = appInsights.id

@description('Log Analytics workspace resource ID')
output logAnalyticsWorkspaceId string = logAnalytics.id

@description('Action Group resource ID')
output actionGroupId string = actionGroup.id
