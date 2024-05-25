targetScope = 'resourceGroup'

param name string
param environment string
param containerAppsEnvironmentId string
param containerRegistryLoginServer string
param containerRegistryName string
@secure()
param containerRegistryPassword string
param location string = resourceGroup().location
param containerApppName string = 'app-${name}-${environment}' 

@description('This module seeds the ACR with the public version of the app')
module acrImportImage 'br/public:deployment-scripts/import-acr:3.0.1' = {
  name: 'importContainerImage'
  params: {
    acrName: containerRegistryName
    location: location
    images: array('mcr.microsoft.com/azuredocs/containerapps-helloworld:latest')
  }
}

resource containerApps 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: containerApppName
  location: location
  properties: {
    workloadProfileName: 'Consumption'
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      }
      activeRevisionsMode: 'Single'      
      registries: [
        {
          server: containerRegistryLoginServer
          username: containerRegistryName
          passwordSecretRef: '${containerRegistryName}-password'
        }
      ]
      secrets: [
        {
          name: '${containerRegistryName}-password'
          value: containerRegistryPassword
        }
      ]      
    }    
          
    template: {
      revisionSuffix: 'firstversion'
      containers: [
        {
          name: containerApppName      
          image: acrImportImage.outputs.importedImages[0].acrHostedImage               
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

output containerAppsName string = containerApps.name
