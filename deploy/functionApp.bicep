targetScope = 'resourceGroup'

// The API runs on Flex Consumption as a custom handler: the Functions host starts the
// unchanged ASP.NET Core app (run.sh) and proxies every request to it. Flex draws instances
// from a pre-provisioned pool, which is what makes the difference that motivated the move —
// a measured cold start of ~3.6s against ~23s on Container Apps, where 13-22s of every wake
// went on scheduling, image pull and sandbox creation before the process even started.

@description('Application name, e.g. ccdiary-dev.')
param appName string

param location string = resourceGroup().location

@description('Storage account holding the application data, the Functions host state and the deployment package.')
param storageAccountName string

@description('Blob container the deployment package is read from.')
param deploymentContainerName string

@description('Browser origins allowed to call the API.')
param allowedOrigins array = []

@description('Non-secret application settings, as a name/value map.')
param appSettings object = {}

@description('Settings whose value lives in Key Vault, as a map of setting name to secret URI.')
// Holds secret URIs, not secret values: the linter matches on the parameter name alone.
#disable-next-line secure-secrets-in-params
param secretSettingUris object = {}

@description('Instance memory in MB. 512 gives a quarter vCPU, 2048 a full one, 4096 two.')
param instanceMemoryMB int = 2048

@description('Ceiling on on-demand instances. Billing is per active execution, so this caps a runaway, not a bill.')
param maximumInstanceCount int = 4

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

// FC1 is the only SKU in the plan, and Flex allows exactly one app per plan.
resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: 'asp-${appName}'
  location: location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

// Identity-based host storage, so the account keeps shared-key access disabled. The three
// data-plane roles this needs are granted in resourceGroup.bicep.
var hostSettings = [
  {
    name: 'AzureWebJobsStorage__accountName'
    value: storageAccountName
  }
]

var plainSettings = map(items(appSettings), item => {
  name: item.key
  value: item.value
})

// A Key Vault reference keeps the credential out of the site's own configuration: an inline
// value is readable by anyone who can run `az functionapp config appsettings list`, which is
// the same exposure that container app secrets existed to avoid.
var keyVaultSettings = map(items(secretSettingUris), item => {
  name: item.key
  value: '@Microsoft.KeyVault(SecretUri=${item.value})'
})

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: 'func-${appName}'
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      // This template is authoritative for the settings: ARM replaces the whole collection
      // on every deployment, so anything not declared here is removed. The infrastructure
      // script therefore passes the complete set, and the deploy workflows set none.
      appSettings: union(hostSettings, plainSettings, keyVaultSettings)
      // CORS has to be configured at the platform, not left to the application. The Functions
      // front end answers preflight OPTIONS itself and never forwards it to the handler, so
      // the app's own CORS middleware never sees it and the browser blocks every authenticated
      // call. Measured on the spike: without this, preflight returned 204 with no
      // Access-Control-Allow-Origin at all.
      cors: {
        allowedOrigins: allowedOrigins
        supportCredentials: false
      }
    }
    functionAppConfig: {
      // Flex runs the app from a zip in this container rather than a mounted file share.
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            type: 'SystemAssignedIdentity'
          }
        }
      }
      // 'custom' is the custom-handler stack. Note the CLI reports its version as '' and
      // rejects `--runtime-version 1.0`, while the site itself records 1.0 as below.
      runtime: {
        name: 'custom'
        version: '1.0'
      }
      scaleAndConcurrency: {
        instanceMemoryMB: instanceMemoryMB
        maximumInstanceCount: maximumInstanceCount
      }
    }
  }
}

output functionAppName string = functionApp.name
output functionAppUrl string = functionApp.properties.defaultHostName

// Granted the storage and Key Vault data-plane roles by the parent template.
output functionAppPrincipalId string = functionApp.identity.principalId
