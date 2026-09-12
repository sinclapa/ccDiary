// Grants a principal read access to the vault's secret values.
//
// Separate module for the same reason as storageRoleAssignments.bicep: a role assignment's
// name must be computable at the start of the deployment, and the function app's principal
// id is a module output. Passing it in as a parameter resolves it before this nested
// deployment begins; assigning it inline fails with BCP120.

param principalId string
param keyVaultName string

@description('Principal type; ServicePrincipal for a managed identity, User for a developer.')
param principalType string = 'ServicePrincipal'

@description('Key Vault Secrets User by default: read a secret value, and nothing else.')
param roleDefinitionId string = '4633458b-17de-408a-b874-0445c86b69e6'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource secretsRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, principalId, roleDefinitionId)
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
    principalId: principalId
    principalType: principalType
  }
}
