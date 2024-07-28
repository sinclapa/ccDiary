targetScope='subscription'

@minLength(5)
@maxLength(87)
param name string
param environment string
param adminUser string
param adminUserSID string
param location string = deployment().location

resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${name}-${environment}'
  location: location
}

module resourceGroupModule 'resourceGroup.bicep' = {
  name: 'resourceGroupTemplate'
  scope: resourceGroup
  params: {
    name: name
    environment: environment
    adminUser: adminUser
    adminUserSID: adminUserSID
    location: resourceGroup.location    
  }  
}

output resourceGroupName string = resourceGroup.name
output containerAppName string = resourceGroupModule.outputs.containerAppName
output containerAppUrl string = resourceGroupModule.outputs.containerAppUrl
output containerRegistryName string = resourceGroupModule.outputs.containerRegistryName
output containerRegistryLoginServer string = resourceGroupModule.outputs.containerRegistryLoginServer
output databaseServer string = resourceGroupModule.outputs.databaseServer
output databaseName string = resourceGroupModule.outputs.databaseName
output staticSiteName string = resourceGroupModule.outputs.staticSiteName
output staticSiteUrl string = resourceGroupModule.outputs.staticSiteUrl
output entraApplicationIdURI string = resourceGroupModule.outputs.entraApplicationIdURI
output entraClientId string = resourceGroupModule.outputs.entraClientId
output entraTenantId string = resourceGroupModule.outputs.entraTenantId
output ccdiaryApiImageExists bool = resourceGroupModule.outputs.ccdiaryApiImageExists
