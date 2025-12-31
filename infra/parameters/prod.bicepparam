// Production Environment Parameters
using '../main.bicep'

// Environment configuration
param environmentName = 'prod'
param location = 'japaneast'
param projectName = 'comical'

// Tags for production resources
param tags = {
  costCenter: 'Production'
  owner: 'OpsTeam'
  purpose: 'Production Workload'
  criticality: 'High'
}

// Git tag - set by CI/CD pipeline or leave empty
param gitTag = ''

// PostgreSQL configuration for prod environment
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

// GitHub configuration for Static Web Apps
// Note: These should be provided securely via GitHub Secrets
param githubToken = ''  // Must be provided during deployment
param repositoryUrl = 'https://github.com/Takas0522/ComiCal'
param repositoryBranch = 'main'
