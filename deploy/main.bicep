// https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations

targetScope='subscription'

@minLength(5)
@maxLength(20)
param name string
param adminUser string
param adminUserSID string
param devApiContainerImage string
param location string = deployment().location

resource resourceGroupDev 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${name}-Dev'
  location: location
}

module resourceGroupModuleDev 'resourceGroup.bicep' = {
  name: 'resourceGroupDev'
  scope: resourceGroupDev
  params: {
    name: name
    environment: 'Dev'
    adminUser: adminUser
    adminUserSID: adminUserSID
    containerImageName: devApiContainerImage
    location: location    
  }  
}

output devEnvironment object = resourceGroupModuleDev.outputs
