using '../main.bicep'

// sqlAdminPassword is a @secure() param — supply via CI/CD:
//   az deployment sub create ... --parameters sqlAdminPassword="$(SQL_ADMIN_PASSWORD)"

param env = 'dev'
param location = 'japaneast'
param sqlVCores = 1
param logRetentionDays = 30
param enablePurgeProtection = false
param alertWebhookUrl = ''
