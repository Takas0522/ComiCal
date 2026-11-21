// Grants the maintainer who runs the daily batch on their PC (tools/run-batch-local.ps1)
// the data-plane roles it needs. The batch authenticates with DefaultAzureCredential (az login).
// Deployable on its own: plan/scripts/apply-04-batch-local-setup.ps1 uses this same file, so the
// role assignment names (deterministic guid()) match what main.bicep deploys later.

@description('Storage account that stores cover images')
param storageAccountName string

@description('Key Vault that stores the Rakuten API credentials')
param keyVaultName string

@description('Entra object ID of the batch operator (a user)')
param principalId string

var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource blobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storage
  name: guid(storage.id, principalId, storageBlobDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: principalId
    principalType: 'User'
  }
}

resource kvSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: kv
  name: guid(kv.id, principalId, kvSecretsUserRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: principalId
    principalType: 'User'
  }
}
