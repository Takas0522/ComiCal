using '../main.bicep'

// Secure parameters are supplied via CI/CD environment variables.

param env = 'dev'
param location = 'japaneast'
param sqlVCores = 1
param logRetentionDays = 30
param enablePurgeProtection = false
param alertWebhookUrl = ''
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param rakutenApplicationId = readEnvironmentVariable('RAKUTEN_APPLICATION_ID')
param rakutenAccessKey = readEnvironmentVariable('RAKUTEN_ACCESS_KEY')
param rakutenAffiliateId = readEnvironmentVariable('RAKUTEN_AFFILIATE_ID')
