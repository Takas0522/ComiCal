using '../main.bicep'

// Secure parameters are supplied via CI/CD environment variables.
// alertWebhookUrl should be set to the production Slack/Teams incoming webhook URL.

param env = 'prod'
param location = 'japaneast'
param sqlVCores = 2
param logRetentionDays = 90
param enablePurgeProtection = true
param alertWebhookUrl = ''
param alertEmailAddress = readEnvironmentVariable('ALERT_EMAIL_ADDRESS', '')
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param rakutenApplicationId = readEnvironmentVariable('RAKUTEN_APPLICATION_ID')
param rakutenAccessKey = readEnvironmentVariable('RAKUTEN_ACCESS_KEY')
param rakutenAffiliateId = readEnvironmentVariable('RAKUTEN_AFFILIATE_ID')

// Daily batch runs at the default schedule (03:00 JST / 18:00 UTC, warmup 02:50 JST / 17:50 UTC).
// dev is offset +6h from this (see infra/params/dev.bicepparam) so the two environments — which
// share the same Rakuten API credentials — never call the Rakuten Books API concurrently.
