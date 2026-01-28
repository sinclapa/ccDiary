targetScope = 'resourceGroup'

param appName string
param containerAppsEnvironmentId string
param containerImageName string
var location string = resourceGroup().location

resource containerApps 'Microsoft.App/containerApps@2024-03-01' = {
  name: toLower('ca-${appName}')
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
      } 
    }    
          
    template: {
      containers: [
        {
          image: containerImageName
          name: toLower('ca-${appName}')
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

output containerAppId string = containerApps.id
output containerAppName string = containerApps.name
output containerAppUrl string = containerApps.properties.configuration.ingress.fqdn
