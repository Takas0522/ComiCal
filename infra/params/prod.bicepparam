using '../main.bicep'

// sqlAdminPassword is a @secure() param — supply via CI/CD env var:
//   SQL_ADMIN_PASSWORD=... az deployment sub create -p infra/params/prod.bicepparam ...
// alertWebhookUrl should be set to the production Slack/Teams incoming webhook URL.

param env = 'prod'
param location = 'japaneast'
param sqlVCores = 2
param logRetentionDays = 90
param enablePurgeProtection = true
param alertWebhookUrl = ''
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD')
param sqlEntraAdminLogin = readEnvironmentVariable('SQL_ENTRA_ADMIN_LOGIN', 'comical-github-oidc')
param sqlEntraAdminObjectId = readEnvironmentVariable('SQL_ENTRA_ADMIN_OBJECT_ID')
