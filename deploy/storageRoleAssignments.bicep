// Grants a principal the storage data-plane roles the application needs.
//
// This lives in its own module because a role assignment's name must be computable at
// the start of the deployment, and the Container App's principal id is a module output.
// Passing it in as a parameter resolves it before this nested deployment begins.
//
// Note these are *data-plane* roles. Control-plane roles such as Storage Account
// Contributor grant no access to the tables or blobs themselves, and the two planes are
// separate grants — missing either one surfaces as a failing health check rather than a
// deployment error.

param principalId string
param storageAccountName string

@description('Principal type; ServicePrincipal for a managed identity, User for a developer.')
param principalType string = 'ServicePrincipal'

@description('''Role definition ids to grant. Defaults to what the application itself needs:
Storage Table Data Contributor and Storage Blob Data Contributor. The function app asks for
more, because the Functions host keeps its own leases and queues on the same account.''')
param roleDefinitionIds array = [
  '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' // Storage Table Data Contributor
  'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
]

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// Named by the same guid() formula as the separate resources this replaced, so re-running
// against an environment deployed by the earlier template reuses the assignments rather
// than creating duplicates.
resource dataRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for roleDefinitionId in roleDefinitionIds: {
    scope: storageAccount
    name: guid(storageAccount.id, principalId, roleDefinitionId)
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
      principalId: principalId
      principalType: principalType
    }
  }
]
