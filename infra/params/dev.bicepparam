using '../main.bicep'

// Secure parameters are supplied via CI/CD environment variables.

param env = 'dev'
param location = 'japaneast'
param sqlVCores = 1
param logRetentionDays = 30
param enablePurgeProtection = false
param alertWebhookUrl = ''
param alertEmailAddress = readEnvironmentVariable('ALERT_EMAIL_ADDRESS', '')
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param rakutenApplicationId = readEnvironmentVariable('RAKUTEN_APPLICATION_ID')
param rakutenAccessKey = readEnvironmentVariable('RAKUTEN_ACCESS_KEY')
param rakutenAffiliateId = readEnvironmentVariable('RAKUTEN_AFFILIATE_ID')

// dev/prod share the same Rakuten API application credentials (same allowlisted IPs, same
// Application ID). Offset dev's daily batch by +6h from prod's default (03:00 JST) so the two
// environments never call the Rakuten Books API at the same time: dev runs at 09:00 JST.
// Warmup stays paired 10 minutes ahead of the daily run.
param dailyBatchCronExpression = '0 0 0 * * *' // 00:00 UTC = 09:00 JST
param warmupBatchCronExpression = '0 50 23 * * *' // 23:50 UTC (previous day) = 08:50 JST
