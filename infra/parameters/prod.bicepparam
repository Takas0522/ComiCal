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
