// https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations

targetScope='subscription'

@minLength(5)
@maxLength(20)
param name string

param environment string
param externalDomainName string?

@description('Non-secret application settings for the function app, as a name/value map.')
param functionAppSettings object = {}

@description('Function app settings whose value lives in Key Vault, as a map of setting name to secret URI.')
// Holds secret URIs, not secret values: the linter matches on the parameter name alone.
#disable-next-line secure-secrets-in-params
param functionAppSecretUris object = {}

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
    externalDomainName: externalDomainName
    functionAppSettings: functionAppSettings
    functionAppSecretUris: functionAppSecretUris
    location: location
  }
}

output environment object = resourceGroupModule.outputs
