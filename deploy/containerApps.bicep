targetScope = 'resourceGroup'

param appName string
param containerAppsEnvironmentId string
param containerImageName string

@description('Plain environment variables currently set on a deployed container app, as a name/value map. Empty on a first deployment.')
@secure()
param existingEnvVars object = {}

@description('Environment variables backed by a container app secret, as a map of variable name to secret name.')
// Holds secret names, not secret values: the linter matches on the parameter name alone.
// Left non-secure deliberately so what-if can still evaluate the resulting env array.
#disable-next-line secure-secrets-in-params
param existingSecretRefs object = {}

@description('Container app secrets currently configured, as a name/value map. Empty on a first deployment.')
@secure()
param existingSecrets object = {}

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

// Secret-backed variables carry a secretRef instead of a value, so they need preserving
// separately — and the secrets themselves must be declared too. The template not declaring
// `secrets` is a deletion as far as ARM is concerned, which would leave every secretRef
// pointing at nothing and the revision unable to start.
var preservedSecretEnv = [for item in items(existingSecretRefs): {
  name: item.key
  secretRef: item.value
}]

var containerEnv = union(defaultEnv, preservedEnv, preservedSecretEnv)

var preservedSecrets = [for item in items(existingSecrets): {
  name: item.key
  value: item.value
}]

resource containerApps 'Microsoft.App/containerApps@2025-01-01' = {
  name: toLower('ca-${appName}')
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: preservedSecrets
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
          // Sized for cold start rather than throughput. The app idles at zero replicas, so
          // CPU is only billed while a replica is actually up — and nearly all of that time
          // is .NET runtime init and JIT, which is CPU bound. A quarter vCPU made every wake
          // roughly four times longer than it needed to be for no saving, because the whole
          // month's usage sits inside the free grant either way.
          resources: {
            cpu: json('1.0')
            memory: '2.0Gi'
          }
          env: containerEnv
          // Without a probe the platform falls back to a TCP check on the target port, and
          // Kestrel binds before StorageBootstrapper runs — so ingress starts routing while
          // the tables and containers are still being created, and the first request can see
          // a DOWN health check complaining the app info row is missing. Gating readiness on
          // the actuator instead means traffic waits for bootstrap. Poll fast: bootstrap
          // takes ~2s, and every second spent probing is a second added to a user's wait.
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/actuator/health'
                port: 8080
                scheme: 'HTTP'
              }
              periodSeconds: 2
              failureThreshold: 10
              timeoutSeconds: 5
            }
          ]
        }
      ]
      // No liveness probe deliberately: maxReplicas is 1, so a false positive has nothing to
      // fail over to and would take the app down rather than heal it.
      scale: {
        minReplicas: 0
        maxReplicas: 1
        // The wake is expensive and the app is read-heavy, so the thing worth minimising is
        // how often it is paid, not just how long it takes. At the 300s default a five minute
        // pause mid-read costs another full cold start; 30 minutes means roughly one per
        // browsing session. Still inside the monthly free grant at this traffic level.
        cooldownPeriod: 1800
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
