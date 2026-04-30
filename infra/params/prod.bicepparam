using '../main.bicep'

// sqlAdminPassword is a @secure() param — supply via CI/CD:
//   az deployment sub create ... --parameters sqlAdminPassword="$(SQL_ADMIN_PASSWORD)"
// alertWebhookUrl should be set to the production Slack/Teams incoming webhook URL.

param env = 'prod'
param location = 'japaneast'
param sqlVCores = 2
param logRetentionDays = 90
param enablePurgeProtection = true
param alertWebhookUrl = ''
