// https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations

targetScope='subscription'

@minLength(5)
@maxLength(20)
param name string

param environment string
param devApiContainerImage string
param externalDomainName string?

@description('Plain environment variables currently set on the deployed container app, preserved across redeployments. Empty on a first deployment.')
@secure()
param existingEnvVars object = {}

@description('Environment variables backed by a container app secret, as a map of variable name to secret name.')
// Holds secret names, not secret values: the linter matches on the parameter name alone.
// Left non-secure deliberately so what-if can still evaluate the resulting env array.
#disable-next-line secure-secrets-in-params
param existingSecretRefs object = {}

@description('Container app secrets currently configured, preserved across redeployments.')
@secure()
param existingSecrets object = {}
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
    existingSecretRefs: existingSecretRefs
    existingSecrets: existingSecrets
    location: location
  }
}

output environment object = resourceGroupModule.outputs
