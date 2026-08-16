targetScope = 'resourceGroup'

param appName string
param containerAppsEnvironmentId string
param containerImageName string

@description('Environment variables currently set on a deployed container app, as a name/value map. Empty on a first deployment.')
@secure()
param existingEnvVars object = {}

var location string = resourceGroup().location

// This template is authoritative for the container spec, so anything it does not declare is
// erased on deployment. Most of the application configuration cannot be declared here: it
// depends on outputs this very deployment produces (the container and static site FQDNs feed
// the Entra app registration, which in turn yields the client id and secret), so the script
// applies it afterwards. That left a redeployment dropping a running app to the single
// variable below — and since the app now fails fast without Storage__AccountName, and
// ingress sends 100% of traffic to the latest revision, the revision failed to activate and
// took the environment down until the script caught up. Feeding the running values back in
// keeps the redeployed revision identical to the one already serving.
//
// union() de-duplicates, so a preserved copy of DisableHttpsRedirection collapses into the
// default rather than producing a duplicate name; the script never changes its value.
var defaultEnv = [
  {
    name: 'DisableHttpsRedirection'
    value: 'true'
  }
]

var preservedEnv = [for item in items(existingEnvVars): {
  name: item.key
  value: item.value
}]

var containerEnv = union(defaultEnv, preservedEnv)

resource containerApps 'Microsoft.App/containerApps@2024-03-01' = {
  name: toLower('ca-${appName}')
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
        clientCertificateMode: 'Ignore'
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
          env: containerEnv
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output containerAppId string = containerApps.id
output containerAppName string = containerApps.name
output containerAppUrl string = containerApps.properties.configuration.ingress.fqdn

// The system-assigned identity is what lets the app reach storage without holding a
// secret; the resource group template grants it the data-plane roles.
output containerAppPrincipalId string = containerApps.identity.principalId
