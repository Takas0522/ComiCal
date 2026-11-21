using '../main.bicep'

// Secure parameters are supplied via CI/CD environment variables.
// alertWebhookUrl should be set to the production Slack/Teams incoming webhook URL.

param env = 'prod'
param location = 'japaneast'
param sqlVCores = 2
// Requires the database to have been migrated to the free offer first
// (plan/scripts/apply-07-sql-free-offer.ps1). An existing paid database cannot be converted.
param sqlUseFreeLimit = true
param logRetentionDays = 30
param logDailyQuotaGb = '0.2'
param enablePurgeProtection = true
param alertWebhookUrl = ''
param alertEmailAddress = readEnvironmentVariable('ALERT_EMAIL_ADDRESS', '')
param monthlyBudgetAmount = 3000
param budgetStartDate = '2026-11-01'
// Entra object ID of the maintainer who runs tools/run-batch-local.ps1 (GitHub variable).
param batchOperatorPrincipalId = readEnvironmentVariable('BATCH_OPERATOR_PRINCIPAL_ID', '')
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param rakutenApplicationId = readEnvironmentVariable('RAKUTEN_APPLICATION_ID')
param rakutenAccessKey = readEnvironmentVariable('RAKUTEN_ACCESS_KEY')
param rakutenAffiliateId = readEnvironmentVariable('RAKUTEN_AFFILIATE_ID')
