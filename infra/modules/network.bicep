// ============================================================================
// network.bicep - Networking placeholder
// ----------------------------------------------------------------------------
// MVP does not use VNet integration / Private Endpoints (cost-minimal design).
// This module exists to reserve the integration point and the
// `privateEndpointEnabled` switch for future use without changing main.bicep.
// AVM note: when this module starts deploying real VNets/PEs, prefer
//   `br/public:avm/res/network/virtual-network` and
//   `br/public:avm/res/network/private-endpoint`.
// ============================================================================

// Note: parameters below are intentionally retained (unused in MVP) so the
// public signature of this module stays stable when network resources are
// added. They are echoed to outputs to satisfy the linter.

@description('Common short prefix for all resource names.')
param prefix string

@description('Environment short code (dev / prod).')
param env string

@description('Short region code embedded in resource names.')
param regionShort string

@description('Azure region.')
param location string

@description('Reserved switch to enable Private Endpoints. MVP=false.')
param privateEndpointEnabled bool = false

@description('Common tags applied to every deployed resource.')
param tags object = {}

// ----------------------------------------------------------------------------
// No resources are deployed in MVP. The following block is the future shape:
// ----------------------------------------------------------------------------
// resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = if (privateEndpointEnabled) {
//   name: '${prefix}-${env}-${regionShort}-vnet'
//   location: location
//   tags: tags
//   properties: {
//     addressSpace: { addressPrefixes: [ '10.20.0.0/16' ] }
//     subnets: [
//       { name: 'snet-pe',   properties: { addressPrefix: '10.20.0.0/24' } }
//       { name: 'snet-func', properties: { addressPrefix: '10.20.1.0/24' } }
//     ]
//   }
// }
// ----------------------------------------------------------------------------

@description('Whether Private Endpoint integration is enabled (always false in MVP).')
output privateEndpointEnabled bool = privateEndpointEnabled

@description('Reserved VNet name (empty until network resources are deployed).')
output virtualNetworkName string = ''

@description('Echo of the prefix parameter (placeholder until VNet resources are added).')
output reservedPrefix string = prefix

@description('Echo of the env parameter (placeholder until VNet resources are added).')
output reservedEnv string = env

@description('Echo of the region-short parameter (placeholder until VNet resources are added).')
output reservedRegionShort string = regionShort

@description('Echo of the location parameter (placeholder until VNet resources are added).')
output reservedLocation string = location

@description('Echo of the tags parameter (placeholder until VNet resources are added).')
output reservedTags object = tags
