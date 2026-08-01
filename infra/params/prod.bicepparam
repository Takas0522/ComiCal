using '../main.bicep'

// Secure parameters are supplied via CI/CD environment variables.
// alertWebhookUrl should be set to the production Slack/Teams incoming webhook URL.

param env = 'prod'
param location = 'japaneast'
param sqlVCores = 2
param logRetentionDays = 90
param enablePurgeProtection = true
param alertWebhookUrl = ''
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param rakutenApplicationId = readEnvironmentVariable('RAKUTEN_APPLICATION_ID')
param rakutenAccessKey = readEnvironmentVariable('RAKUTEN_ACCESS_KEY')
param rakutenAffiliateId = readEnvironmentVariable('RAKUTEN_AFFILIATE_ID')
