// https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations

targetScope='subscription'

@minLength(5)
@maxLength(20)
param name string

param environment string
param adminUser string
param adminUserSID string
param devApiContainerImage string
param externalDomainName string = ''
param location string = deployment().location

resource resourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: 'rg-${name}-${environment}'
  location: location
}

module resourceGroupModule 'resourceGroup.bicep' = {
  name: 'resourceGroupModule-${environment}'
  scope: resourceGroup
  params: {
    name: name
    environment: environment
    adminUser: adminUser
    adminUserSID: adminUserSID
    containerImageName: devApiContainerImage
    externalDomainName: externalDomainName
    location: location
  }
}

output environment object = resourceGroupModule.outputs
