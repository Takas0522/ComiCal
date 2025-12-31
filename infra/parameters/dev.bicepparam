// Development Environment Parameters
using '../main.bicep'

// Environment configuration
param environmentName = 'dev'
param location = 'eastus2'  // japaneastから変更 - VMクォータ制限回避
param projectName = 'comical'

// Tags for development resources
param tags = {
  costCenter: 'Development'
  owner: 'DevTeam'
  purpose: 'Development and Testing'
}

// Git tag - set by CI/CD pipeline or leave empty
param gitTag = ''

// PostgreSQL configuration for dev environment
// Note: In production deployment, these should be provided securely via GitHub Secrets or Azure Key Vault
param postgresAdminUsername = 'psqladmin'
param postgresAdminPassword = ''  // Must be provided during deployment

// Azure AD Admin configuration (optional, can be set via GitHub Actions)
param postgresAadAdminObjectId = ''
param postgresAadAdminPrincipalName = ''
param postgresAadAdminPrincipalType = 'User'

// Security configuration
// Note: These should be provided securely via GitHub Secrets
param rakutenApiKey = ''  // Must be provided during deployment

// RBAC configuration  
param skipRbacAssignments = true  // Service Principalに権限不足のため一時的にスキップ
