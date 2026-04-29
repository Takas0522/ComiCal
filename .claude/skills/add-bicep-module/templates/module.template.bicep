@description('Environment short code (dev/prod)')
param env string

@description('Azure region')
param location string = 'japaneast'

@description('Common prefix for resource naming')
param prefix string = 'comical'

@description('Resource tags')
param tags object = {
  env: env
  app: 'comical'
}

var resourceName = '${prefix}-${env}-jpe-{{kind}}'

// TODO: Define resource here

@description('Resource ID')
output resourceId string = ''

@description('Resource name')
output resourceName string = resourceName
