targetScope='subscription'

@minLength(5)
@maxLength(87)
param name string
param environment string
param adminUser string
param adminUserSID string
param imageName string = 'azureapi'
param location string = deployment().location
param isContainerImagePresent bool = false

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
    imageName: imageName
    location: resourceGroup.location
    isContainerImagePresent: isContainerImagePresent    
  }  
}

output resourceGroupName string = resourceGroup.name
output containerAppName string = resourceGroupModule.outputs.containerAppName
output containerRegistryName string = resourceGroupModule.outputs.containerRegistryName
output containerRegistryLoginServer string = resourceGroupModule.outputs.containerRegistryLoginServer
output databaseServer string = resourceGroupModule.outputs.databaseServer
output databaseName string = resourceGroupModule.outputs.databaseName
