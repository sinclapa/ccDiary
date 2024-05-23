targetScope = 'resourceGroup'

param name string
param environment string
param imageName string
param containerAppsEnvironmentId string
param containerRegistryLoginServer string
param containerRegistryName string
@secure()
param containerRegistryPassword string
param location string = resourceGroup().location

resource containerApps 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: 'app-${name}-${environment}'
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
      containers: [
        {
          name: 'api'          
          image: '${containerRegistryLoginServer}/${imageName}:latest'                    
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
