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

var storageTableDataContributorRoleId string = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
var storageBlobDataContributorRoleId string = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource tableDataRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount
  name: guid(storageAccount.id, principalId, storageTableDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      storageTableDataContributorRoleId
    )
    principalId: principalId
    principalType: principalType
  }
}

resource blobDataRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount
  name: guid(storageAccount.id, principalId, storageBlobDataContributorRoleId)
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      storageBlobDataContributorRoleId
    )
    principalId: principalId
    principalType: principalType
  }
}
