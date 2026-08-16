// https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations

targetScope='subscription'

@minLength(5)
@maxLength(20)
param name string

param environment string
param devApiContainerImage string
param externalDomainName string?

@description('Environment variables currently set on the deployed container app, preserved across redeployments. Empty on a first deployment.')
@secure()
param existingEnvVars object = {}
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
    containerImageName: devApiContainerImage
    externalDomainName: externalDomainName
    existingEnvVars: existingEnvVars
    location: location
  }
}

output environment object = resourceGroupModule.outputs
