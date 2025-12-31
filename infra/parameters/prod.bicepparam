// Production Environment Parameters
using '../main.bicep'

// Environment configuration
param environmentName = 'prod'
param location = 'japaneast'
param projectName = 'comical'

// PostgreSQL configuration
// Note: Both postgresAdminLogin and postgresAdminPassword should be provided at deployment time
// via command line or Key Vault for security
// Example: --parameters postgresAdminLogin=comicaladmin postgresAdminPassword=<secure-password>

param databaseName = 'comical'

// Tags for production resources
param tags = {
  costCenter: 'Production'
  owner: 'OpsTeam'
  purpose: 'Production Workload'
  criticality: 'High'
}

// Git tag - set by CI/CD pipeline or leave empty
param gitTag = ''
