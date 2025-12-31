// Development Environment Parameters
using '../main.bicep'

// Environment configuration
param environmentName = 'dev'
param location = 'japaneast'
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
