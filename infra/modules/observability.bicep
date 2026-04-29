// ============================================================================
// observability.bicep - Log Analytics + App Insights + Action Group
// ----------------------------------------------------------------------------
// Resources:
//   - Log Analytics Workspace          (Microsoft.OperationalInsights/workspaces)
//   - Application Insights             (workspace-based)
//   - Action Group                     (email + optional webhook receivers)
//
// Alert rules and workbooks are owned by sibling modules (alerts.bicep,
// workbook.bicep) so this module remains the single source of truth for the
// telemetry plane while the alerting plane evolves independently.
// AVM note:
//   `avm/res/operational-insights/workspace` and `avm/res/insights/component`
//   exist, but inline authoring keeps the surface explicit and minimises
//   transient AVM parameter churn.
// ============================================================================

@description('Common short prefix for all resource names.')
param prefix string

@description('Environment short code (dev / prod).')
param env string

@description('Short region code embedded in resource names.')
param regionShort string

@description('Azure region.')
param location string

@description('Log Analytics retention in days.')
@minValue(30)
@maxValue(730)
param logRetentionDays int

@description('Email addresses notified by the Action Group. Empty array = portal-only (no email). prod environments should supply at least one address via bicepparam.')
param actionGroupEmails array = []

@secure()
@description('Optional Slack/Teams Webhook URL for the Action Group. Empty string disables the webhook receiver.')
param alertWebhookUrl string = ''

@description('Common tags applied to every deployed resource.')
param tags object = {}

var lawName = '${prefix}-${env}-${regionShort}-log'
var aiName = '${prefix}-${env}-${regionShort}-appi'
var agName = 'comical-${env}-actiongroup'
var hasWebhook = !empty(alertWebhookUrl)

// ----------------------------------------------------------------------------
// Log Analytics Workspace
// ----------------------------------------------------------------------------

resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: lawName
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: logRetentionDays
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
}

// ----------------------------------------------------------------------------
// Application Insights (workspace-based)
// ----------------------------------------------------------------------------

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: aiName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: law.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ----------------------------------------------------------------------------
// Action Group
// ----------------------------------------------------------------------------

var emailReceivers = [for (addr, idx) in actionGroupEmails: {
  name: 'email-${idx}'
  emailAddress: addr
  useCommonAlertSchema: true
}]

resource actionGroup 'Microsoft.Insights/actionGroups@2024-10-01-preview' = {
  name: agName
  location: 'global'
  tags: tags
  properties: {
    groupShortName: take('${prefix}${env}ag', 12)
    enabled: true
    emailReceivers: emailReceivers
    webhookReceivers: hasWebhook ? [
      {
        name: 'webhook-primary'
        serviceUri: alertWebhookUrl
        useCommonAlertSchema: true
      }
    ] : []
  }
}

// ----------------------------------------------------------------------------
// Outputs
// ----------------------------------------------------------------------------

@description('Log Analytics workspace name.')
output logAnalyticsWorkspaceName string = law.name

@description('Log Analytics workspace resource ID.')
output logAnalyticsWorkspaceId string = law.id

@description('Log Analytics workspace customer ID (used for agent configuration).')
output logAnalyticsCustomerId string = law.properties.customerId

@description('Application Insights resource name.')
output appInsightsName string = appInsights.name

@description('Application Insights resource ID.')
output appInsightsId string = appInsights.id

@description('Application Insights connection string (non-secret; safe to surface to App Settings via KV reference).')
output appInsightsConnectionString string = appInsights.properties.ConnectionString

@description('Action Group resource ID.')
output actionGroupId string = actionGroup.id

@description('Action Group resource name.')
output actionGroupName string = actionGroup.name
