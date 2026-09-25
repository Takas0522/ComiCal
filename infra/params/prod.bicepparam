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

// Keep the production schedule explicit so a future main.bicep default change cannot move it.
// dev is offset +6h from this (see infra/params/dev.bicepparam) so the two environments — which
// share the same Rakuten API credentials — never call the Rakuten Books API concurrently.
param dailyBatchCronExpression = '0 0 18 * * *' // 18:00 UTC = 03:00 JST
param warmupBatchCronExpression = '0 50 17 * * *' // 17:50 UTC = 02:50 JST
