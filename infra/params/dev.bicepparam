using '../main.bicep'

// ===== dev environment parameters =====

param prefix      = 'cmcl'
param env         = 'dev'
param region      = 'japaneast'
param regionShort = 'jpe'

// SQL
param sqlTier                  = 'GP_S_Gen5_1'
param sqlAutoPauseDelayMinutes = 60
param sqlAdminLogin            = 'comicaladmin'
// Provided at deploy time via --parameters sqlAdminPassword='...'  (Key Vault preferred)
param sqlAdminPassword         = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')
param sqlAadAdminObjectId      = readEnvironmentVariable('SQL_AAD_ADMIN_OBJECT_ID', '')
param sqlAadAdminLogin         = readEnvironmentVariable('SQL_AAD_ADMIN_LOGIN', '')

// Observability
param logRetentionDays = 30
param alertWebhookUrl  = readEnvironmentVariable('ALERT_WEBHOOK_URL', '')

// Action Group: dev allows empty (portal-only notifications).
param actionGroupEmails = []

// Alert thresholds (lenient for dev)
param apiFailedRequestsThreshold     = 50
param apiP95LatencyMs                = 5000
param sqlDependencyFailuresThreshold = 20
param storageAvailabilityPercent     = 95
param rakutenRateLimitedThreshold    = 200

// Network
param privateEndpointEnabled = false

param tags = {
  project: 'ComiCal'
  env: 'dev'
  managedBy: 'Bicep'
}
