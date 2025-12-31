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
