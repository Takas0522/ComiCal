// Development Environment Parameters
using '../main.bicep'

// Environment configuration
param environmentName = 'dev'
param location = 'japaneast'
param projectName = 'comical'

// PostgreSQL configuration
// Note: Both postgresAdminLogin and postgresAdminPassword should be provided at deployment time
// via command line or Key Vault for security
// Example: --parameters postgresAdminLogin=comicaladmin postgresAdminPassword=<secure-password>

param databaseName = 'comical'

// Tags for development resources
param tags = {
  costCenter: 'Development'
  owner: 'DevTeam'
  purpose: 'Development and Testing'
}

// Git tag - set by CI/CD pipeline or leave empty
param gitTag = ''
