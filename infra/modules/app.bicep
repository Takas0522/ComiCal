@description('Resource name prefix following CAF convention')
param prefix string

@description('Environment short code (dev or prod)')
param env string

@description('Azure region')
param location string

@description('Region for Static Web App (must be one of westus2, centralus, eastus2, westeurope, eastasia). Defaults to eastasia as the closest region to japaneast.')
param swaLocation string = 'eastasia'

@description('SQL Server fully-qualified domain name (from data module)')
param sqlServerFqdn string

@description('SQL Database name (from data module)')
param sqlDatabaseName string

@description('Storage account name (from data module)')
param storageAccountName string

@description('Storage account resource ID used to retrieve access keys for Key Vault secret (from data module)')
param storageAccountId string

@description('Application Insights resource name (from observability module); used to obtain connection string via existing reference')
param appInsightsName string

@description('Enable Key Vault purge protection (false for dev, true for prod)')
param enablePurgeProtection bool

// Rakuten credentials are kept in Key Vault for the locally run batch only. The API Function
// App does not call the Rakuten API (no static egress IP in Azure — see ADR 0004), so its
// search fallback is disabled via NullRakutenBookSearchService.
@secure()
@description('Rakuten Books API application ID')
param rakutenApplicationId string

@secure()
@description('Rakuten Books API access key')
param rakutenAccessKey string

@secure()
@description('Rakuten affiliate ID')
param rakutenAffiliateId string

var kvName = '${prefix}-${env}-jpe-kv'
var swaName = '${prefix}-${env}-jpe-swa'
var funcApiName = '${prefix}-${env}-jpe-func-api'
var planApiName = '${prefix}-${env}-jpe-plan-api'

// Role definition IDs
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
var storageBlobDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'

// Reference existing storage account for role assignments
resource existingStorage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// SQL connection string using Managed Identity — no password required
var sqlConnectionString = 'Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

// Storage connection string stored in Key Vault; Function Apps access it via KV reference
var storageKey = listKeys(storageAccountId, '2023-05-01').keys[0].value
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${storageKey};EndpointSuffix=core.windows.net'

// Reference existing App Insights created by observability module to obtain its connection string
// without exposing it as a module output (secrets must not pass through outputs per policy)
resource existingAppInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: appInsightsName
}

// ──────────────────────────────────────────────────────────────────────────────
// Key Vault
// ──────────────────────────────────────────────────────────────────────────────

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: kvName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    // RBAC mode — access policies are replaced by Azure role assignments
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    // Note: enablePurgeProtection cannot be set to false once true; omit when false
    enablePurgeProtection: enablePurgeProtection ? true : null
  }
}

// Store storage connection string as a Key Vault secret
resource kvSecretStorage 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'AzureWebJobsStorage'
  properties: {
    value: storageConnectionString
  }
}

// Store App Insights connection string as a Key Vault secret
resource kvSecretAppInsights 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'AppInsightsConnectionString'
  properties: {
    value: existingAppInsights.properties.ConnectionString
  }
}

resource kvSecretRakutenAppId 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'RakutenAppId'
  properties: {
    value: rakutenApplicationId
  }
}

resource kvSecretRakutenAccessKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'RakutenAccessKey'
  properties: {
    value: rakutenAccessKey
  }
}

resource kvSecretRakutenAffiliateId 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'RakutenAffiliateId'
  properties: {
    value: rakutenAffiliateId
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// App Service Plan (FlexConsumption, Linux) — API only (the batch runs locally)
// ──────────────────────────────────────────────────────────────────────────────

resource planApi 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planApiName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// Function App — API (SWA-linked)
// ──────────────────────────────────────────────────────────────────────────────

resource funcApi 'Microsoft.Web/sites@2024-04-01' = {
  name: funcApiName
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: planApi.id
    httpsOnly: true
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${existingStorage.properties.primaryEndpoints.blob}app-package-api'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      scaleAndConcurrency: {
        // Personal-use app: cap scale-out so a bug or abusive traffic cannot run up the bill.
        maximumInstanceCount: 10
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      appSettings: [
        {
          name: 'AzureWebJobsStorage__accountName'
          value: storageAccountName
        }
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: '@Microsoft.KeyVault(SecretUri=${kv.properties.vaultUri}secrets/AppInsightsConnectionString/)'
        }
        {
          name: 'SqlConnectionString'
          value: sqlConnectionString
        }
        {
          name: 'StorageAccountUri'
          value: 'https://${storageAccountName}.blob.core.windows.net'
        }
        {
          name: 'BlobBaseUrl'
          value: 'https://${storageAccountName}.blob.core.windows.net/covers'
        }
      ]
    }
  }
  dependsOn: [
    kvSecretAppInsights
  ]
}

// API Function App authentication settings.
// SWA-linked backend forwards anonymous requests to this Function App, and
// each endpoint enforces required-auth checks via x-ms-client-principal
// (see /me/* and /admin/* handlers). We must therefore keep Easy Auth
// "enabled but unauthenticated-allowed" so that requests reach the Function
// code. Without this, Azure Functions returns 401 Bearer challenges before
// our middleware runs.
resource funcApiAuthSettings 'Microsoft.Web/sites/config@2024-04-01' = {
  parent: funcApi
  name: 'authsettingsV2'
  properties: {
    platform: {
      enabled: true
      runtimeVersion: '~1'
    }
    globalValidation: {
      requireAuthentication: false
      unauthenticatedClientAction: 'AllowAnonymous'
    }
    httpSettings: {
      requireHttps: true
      routes: {
        apiPrefix: '/.auth'
      }
      forwardProxy: {
        convention: 'NoProxy'
      }
    }
    identityProviders: {
      azureStaticWebApps: {
        enabled: true
        registration: {
          clientId: reference(swaResourceId, '2024-04-01').defaultHostname
        }
      }
    }
    login: {
      tokenStore: {
        enabled: false
      }
      preserveUrlFragmentsForLogins: false
    }
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// Storage RBAC — grant the API Function App MI access to the storage account
// (host runtime + FlexConsumption deployment package container)
// ──────────────────────────────────────────────────────────────────────────────

resource storageRoleFuncApiBlob 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingStorage
  name: guid(existingStorage.id, funcApi.id, storageBlobDataOwnerRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataOwnerRoleId)
    principalId: funcApi.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource storageRoleFuncApiQueue 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingStorage
  name: guid(existingStorage.id, funcApi.id, storageQueueDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      storageQueueDataContributorRoleId
    )
    principalId: funcApi.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource storageRoleFuncApiTable 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingStorage
  name: guid(existingStorage.id, funcApi.id, storageTableDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      storageTableDataContributorRoleId
    )
    principalId: funcApi.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// Key Vault RBAC — grant "Key Vault Secrets User" to the API Function App MI and SWA MI
// ──────────────────────────────────────────────────────────────────────────────

resource kvRoleAssignFuncApi 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, funcApi.id, kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: funcApi.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// ──────────────────────────────────────────────────────────────────────────────
// Static Web App (Standard) — hosts Angular SSR via Managed Functions
// ──────────────────────────────────────────────────────────────────────────────

// Standard is required for the linked backend (Bring-your-own Functions) and the custom
// Entra External ID registration. Free was evaluated and rejected (see ADR 0004).
resource swaStandard 'Microsoft.Web/staticSites@2024-04-01' = {
  name: swaName
  location: swaLocation
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {}
}

var swaResourceId = resourceId('Microsoft.Web/staticSites', swaName)

// Link the API Function App to the SWA so that /api/* requests are proxied
// with the x-ms-client-principal header injected by SWA authentication.
resource swaLinkedBackend 'Microsoft.Web/staticSites/linkedBackends@2024-04-01' = {
  parent: swaStandard
  name: 'apifunc'
  properties: {
    backendResourceId: funcApi.id
    region: location
  }
}

// SWA Managed Identity → Key Vault Secrets User
resource kvRoleAssignSwa 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, swaResourceId, kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: swaStandard.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

// App Configuration was removed: nothing in the code read it (see ADR 0004).
// Local batch operator RBAC lives in modules/batch-operator.bicep.

// ──────────────────────────────────────────────────────────────────────────────
// Outputs
// ──────────────────────────────────────────────────────────────────────────────

@description('Static Web App default hostname')
output swaDefaultHostname string = reference(swaResourceId, '2024-04-01').defaultHostname

@description('Function App (API) resource name')
output funcApiName string = funcApi.name

@description('Key Vault name')
output kvName string = kv.name

@description('Key Vault URI')
output kvUri string = kv.properties.vaultUri
