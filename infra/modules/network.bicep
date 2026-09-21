@description('Resource name prefix following CAF convention')
param prefix string

@description('Environment short code (dev or prod)')
param env string

@description('Azure region')
param location string

var vnetName = '${prefix}-${env}-jpe-vnet'
var batchSubnetName = '${prefix}-${env}-jpe-snet-batch'
var egressPublicIpName = '${prefix}-${env}-jpe-pip-egress'
var natGatewayName = '${prefix}-${env}-jpe-nat'

resource egressPublicIp 'Microsoft.Network/publicIPAddresses@2024-05-01' = {
  name: egressPublicIpName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Regional'
  }
  properties: {
    publicIPAllocationMethod: 'Static'
    publicIPAddressVersion: 'IPv4'
  }
}

resource natGateway 'Microsoft.Network/natGateways@2024-05-01' = {
  name: natGatewayName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    publicIpAddresses: [
      {
        id: egressPublicIp.id
      }
    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.42.0.0/16'
      ]
    }
    subnets: [
      {
        name: batchSubnetName
        properties: {
          addressPrefix: '10.42.1.0/24'
          natGateway: {
            id: natGateway.id
          }
          delegations: [
            {
              name: 'web-serverfarms'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
    ]
  }
}

@description('Resource ID of the subnet delegated to the batch Function App')
output batchSubnetResourceId string = resourceId('Microsoft.Network/virtualNetworks/subnets', vnet.name, batchSubnetName)

@description('Static public IPv4 address used by the batch Function App for internet egress')
output batchEgressPublicIpAddress string = egressPublicIp.properties.ipAddress
