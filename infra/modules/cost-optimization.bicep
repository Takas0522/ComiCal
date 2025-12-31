// Cost Optimization Module
// This module creates Logic Apps for automatic night shutdown of dev Function Apps
// Weekday: Stop 20:00-08:00 JST, Weekend: Stop all day Saturday/Sunday

@description('Environment name (dev, prod)')
@allowed([
  'dev'
  'prod'
])
param environmentName string

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Project name for resource naming')
param projectName string

@description('Tags to apply to resources')
param tags object = {}

@description('API Function App resource ID')
param apiFunctionAppId string

@description('Batch Function App resource ID')
param batchFunctionAppId string

// Only deploy cost optimization for dev environment
var deployNightShutdown = environmentName == 'dev'

// Variables for naming conventions
var locationAbbreviation = {
  japaneast: 'jpe'
  japanwest: 'jpw'
  eastus: 'eus'
  westus: 'wus'
  westus2: 'wu2'
  centralus: 'cus'
  eastasia: 'eas'
  southeastasia: 'sea'
}

var locationShort = locationAbbreviation[location]

// Logic App Naming: logic-{project}-{purpose}-{env}-{location}
var stopLogicAppName = 'logic-${projectName}-stop-${environmentName}-${locationShort}'
var startLogicAppName = 'logic-${projectName}-start-${environmentName}-${locationShort}'

// Parse resource IDs for Function Apps
var apiFunctionAppSubscriptionId = split(apiFunctionAppId, '/')[2]
var apiFunctionAppResourceGroup = split(apiFunctionAppId, '/')[4]
var apiFunctionAppName = split(apiFunctionAppId, '/')[8]

var batchFunctionAppSubscriptionId = split(batchFunctionAppId, '/')[2]
var batchFunctionAppResourceGroup = split(batchFunctionAppId, '/')[4]
var batchFunctionAppName = split(batchFunctionAppId, '/')[8]

// Logic App to stop Function Apps
// Runs weekdays at 20:00 JST (11:00 UTC) and all day on weekends
resource stopLogicApp 'Microsoft.Logic/workflows@2019-05-01' = if (deployNightShutdown) {
  name: stopLogicAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {}
      triggers: {
        'Recurrence-Weekday-Stop': {
          type: 'Recurrence'
          recurrence: {
            frequency: 'Week'
            interval: 1
            schedule: {
              hours: [
                '11'  // 20:00 JST = 11:00 UTC
              ]
              minutes: [
                0
              ]
              weekDays: [
                'Monday'
                'Tuesday'
                'Wednesday'
                'Thursday'
                'Friday'
              ]
            }
            timeZone: 'UTC'
          }
        }
        'Recurrence-Weekend-Stop': {
          type: 'Recurrence'
          recurrence: {
            frequency: 'Week'
            interval: 1
            schedule: {
              hours: [
                '15'  // Saturday 00:00 JST = Friday 15:00 UTC
              ]
              minutes: [
                0
              ]
              weekDays: [
                'Friday'
              ]
            }
            timeZone: 'UTC'
          }
        }
      }
      actions: {
        'Stop-API-Function-App': {
          type: 'Http'
          inputs: {
            method: 'POST'
            uri: '${environment().resourceManager}subscriptions/${apiFunctionAppSubscriptionId}/resourceGroups/${apiFunctionAppResourceGroup}/providers/Microsoft.Web/sites/${apiFunctionAppName}/stop?api-version=2023-01-01'
            authentication: {
              type: 'ManagedServiceIdentity'
            }
          }
        }
        'Stop-Batch-Function-App': {
          type: 'Http'
          inputs: {
            method: 'POST'
            uri: '${environment().resourceManager}subscriptions/${batchFunctionAppSubscriptionId}/resourceGroups/${batchFunctionAppResourceGroup}/providers/Microsoft.Web/sites/${batchFunctionAppName}/stop?api-version=2023-01-01'
            authentication: {
              type: 'ManagedServiceIdentity'
            }
          }
          runAfter: {
            'Stop-API-Function-App': [
              'Succeeded'
            ]
          }
        }
      }
      outputs: {}
    }
  }
}

// Logic App to start Function Apps
// Runs weekdays at 08:00 JST (23:00 UTC previous day) and Monday morning for weekend stop
resource startLogicApp 'Microsoft.Logic/workflows@2019-05-01' = if (deployNightShutdown) {
  name: startLogicAppName
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    state: 'Enabled'
    definition: {
      '$schema': 'https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#'
      contentVersion: '1.0.0.0'
      parameters: {}
      triggers: {
        'Recurrence-Weekday-Start': {
          type: 'Recurrence'
          recurrence: {
            frequency: 'Week'
            interval: 1
            schedule: {
              hours: [
                '23'  // 08:00 JST = 23:00 UTC (previous day)
              ]
              minutes: [
                0
              ]
              weekDays: [
                'Sunday'    // Monday 08:00 JST
                'Monday'    // Tuesday 08:00 JST
                'Tuesday'   // Wednesday 08:00 JST
                'Wednesday' // Thursday 08:00 JST
                'Thursday'  // Friday 08:00 JST
              ]
            }
            timeZone: 'UTC'
          }
        }
      }
      actions: {
        'Start-API-Function-App': {
          type: 'Http'
          inputs: {
            method: 'POST'
            uri: '${environment().resourceManager}subscriptions/${apiFunctionAppSubscriptionId}/resourceGroups/${apiFunctionAppResourceGroup}/providers/Microsoft.Web/sites/${apiFunctionAppName}/start?api-version=2023-01-01'
            authentication: {
              type: 'ManagedServiceIdentity'
            }
          }
        }
        'Start-Batch-Function-App': {
          type: 'Http'
          inputs: {
            method: 'POST'
            uri: '${environment().resourceManager}subscriptions/${batchFunctionAppSubscriptionId}/resourceGroups/${batchFunctionAppResourceGroup}/providers/Microsoft.Web/sites/${batchFunctionAppName}/start?api-version=2023-01-01'
            authentication: {
              type: 'ManagedServiceIdentity'
            }
          }
          runAfter: {
            'Start-API-Function-App': [
              'Succeeded'
            ]
          }
        }
      }
      outputs: {}
    }
  }
}

// Role assignments for Logic Apps to control Function Apps
// Contributor role is required to start/stop Function Apps
var contributorRoleId = 'b24988ac-6180-42a0-ab88-20f7382dd24c'

resource stopLogicAppRoleAssignmentApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployNightShutdown) {
  name: guid(stopLogicApp!.id, apiFunctionAppId, contributorRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: stopLogicApp!.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource stopLogicAppRoleAssignmentBatch 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployNightShutdown) {
  name: guid(stopLogicApp!.id, batchFunctionAppId, contributorRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: stopLogicApp!.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource startLogicAppRoleAssignmentApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployNightShutdown) {
  name: guid(startLogicApp!.id, apiFunctionAppId, contributorRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: startLogicApp!.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource startLogicAppRoleAssignmentBatch 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (deployNightShutdown) {
  name: guid(startLogicApp!.id, batchFunctionAppId, contributorRoleId)
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', contributorRoleId)
    principalId: startLogicApp!.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output stopLogicAppId string = deployNightShutdown ? stopLogicApp!.id : ''
output stopLogicAppName string = deployNightShutdown ? stopLogicApp!.name : ''
output startLogicAppId string = deployNightShutdown ? startLogicApp!.id : ''
output startLogicAppName string = deployNightShutdown ? startLogicApp!.name : ''
output nightShutdownEnabled bool = deployNightShutdown
