using '../main.bicep'

// sqlAdminPassword is a @secure() param — supply via CI/CD env var:
//   SQL_ADMIN_PASSWORD=... az deployment sub create -p infra/params/dev.bicepparam ...

param env = 'dev'
param location = 'japaneast'
param sqlVCores = 1
param logRetentionDays = 30
param enablePurgeProtection = false
param alertWebhookUrl = ''
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
