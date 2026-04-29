using '../main.bicep'

// ===== prod environment parameters =====

param prefix      = 'cmcl'
param env         = 'prod'
param region      = 'japaneast'
param regionShort = 'jpe'

// SQL
param sqlTier                  = 'GP_S_Gen5_2'
param sqlAutoPauseDelayMinutes = 60
param sqlAdminLogin            = 'comicaladmin'
param sqlAdminPassword         = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
param sqlAadAdminObjectId      = readEnvironmentVariable('SQL_AAD_ADMIN_OBJECT_ID', '')
param sqlAadAdminLogin         = readEnvironmentVariable('SQL_AAD_ADMIN_LOGIN', '')

// Observability
param logRetentionDays = 90
param alertWebhookUrl  = readEnvironmentVariable('ALERT_WEBHOOK_URL', '')

// Action Group: prod must have at least one notification email.
// Replace the placeholder with the on-call distribution list before deploying.
param actionGroupEmails = [
  'oncall@example.com'
]

// Alert thresholds (strict for prod, per docs/specs/oo-init/14-observability-sre.md)
param apiFailedRequestsThreshold     = 10
param apiP95LatencyMs                = 2000
param sqlDependencyFailuresThreshold = 5
param storageAvailabilityPercent     = 99
param rakutenRateLimitedThreshold    = 50

// Network
param privateEndpointEnabled = false

param tags = {
  project: 'ComiCal'
  env: 'prod'
  managedBy: 'Bicep'
}
