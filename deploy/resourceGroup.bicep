targetScope = 'resourceGroup'

param name string
param environment string
param externalDomainName string?
param location string = resourceGroup().location
param containerImageName string

@description('Plain environment variables currently set on the deployed container app, preserved across redeployments. Empty on a first deployment.')
@secure()
param existingEnvVars object = {}

@description('Environment variables backed by a container app secret, as a map of variable name to secret name.')
param existingSecretRefs object = {}

@description('Container app secrets currently configured, preserved across redeployments.')
@secure()
param existingSecrets object = {}
var appName string = '${name}-${environment}'

// Storage account names allow only lowercase alphanumerics and cap at 24 characters.
var storageAccountName string = take(toLower(replace('st${name}${environment}${uniqueString(resourceGroup().id)}', '-', '')), 24)


resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'logs-${appName}'
  location: location
  properties: {
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${appName}'
  location: location
  properties: {
    zoneRedundant: false
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }    
    workloadProfiles: [
      {
        workloadProfileType: 'Consumption'
        name: 'Consumption'
      }
    ]
  }
}

// ---------------------------------------------------------------------------
// Storage: Table + Blob, the application's data store.
//
// Shared-key access is disabled outright — the Container App authenticates with its
// system-assigned identity, so there is no connection string to leak or rotate. That
// does mean `az storage` commands against this account need `--auth-mode login`.
// ---------------------------------------------------------------------------
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  // The account is the resource being accessed, not a caller: the Container App's
  // identity authenticates *to* it. A storage account only needs an identity of its own
  // to reach a key vault for customer-managed keys, which this does not use.
  identity: {
    type: 'None' // NOSONAR (S6378) — see above
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    defaultToOAuthAuthentication: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      keySource: 'Microsoft.Storage'
      // Encrypts a second time at the infrastructure layer, so a flaw in the service
      // level encryption alone does not expose the data. It costs nothing here.
      requireInfrastructureEncryption: true
      services: {
        blob: {
          enabled: true
        }
        table: {
          enabled: true
        }
      }
    }
    networkAcls: {
      defaultAction: 'Allow'
      bypass: 'AzureServices'
    }
  }
}

// Soft delete is the only recovery mechanism available here. Azure SQL provided 7-day
// point-in-time restore; Table Storage has no equivalent at all, so blob-level retention
// plus the in-repo archive are what stand in for it.
resource blobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource containers 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = [
  for containerName in ['images', 'mapcache', 'content']: {
    parent: blobServices
    name: containerName
    properties: {
      publicAccess: 'None'
    }
  }
]

// Automatic cache eviction. The previous SQL implementation never removed expired
// entries, so the cache grew without bound; this is why the tile and route caches were
// moved to blobs, since Table Storage offers nothing equivalent.
resource lifecyclePolicy 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          name: 'expire-map-cache'
          enabled: true
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: ['blockBlob']
              prefixMatch: ['mapcache/tiles', 'mapcache/routes']
            }
            actions: {
              baseBlob: {
                delete: {
                  daysAfterModificationGreaterThan: 90
                }
              }
            }
          }
        }
      ]
    }
  }
}

resource tableServices 'Microsoft.Storage/storageAccounts/tableServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

// Declared here so the infrastructure is self-describing. The application also creates
// them at startup, which is what makes local development against Azurite work.
resource tables 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-05-01' = [
  for tableName in ['diary', 'diaryentry', 'appuser', 'accessrequest', 'appinfo', 'geocodingcache']: {
    parent: tableServices
    name: tableName
  }
]

module storageRoles 'storageRoleAssignments.bicep' = {
  name: 'storage-roles-${appName}'
  params: {
    principalId: containerAppModule.outputs.containerAppPrincipalId
    storageAccountName: storageAccount.name
  }
  dependsOn: [
    tables
    containers
  ]
}

resource staticSite 'Microsoft.Web/staticSites@2023-01-01' = {
  name: 'stapp-${appName}'
  location: location
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'None'
    enterpriseGradeCdnStatus: 'Disabled'
  }
}

resource staticSiteCustomDomain 'Microsoft.Web/staticSites/customDomains@2024-11-01' = if (environment == 'prod' && !empty(externalDomainName ?? '')) {
  parent: staticSite
  name: externalDomainName!
  properties: {}
}

module containerAppModule 'containerApps.bicep' = {
  name: 'containerApps'
  params: {
    appName: appName
    containerAppsEnvironmentId: containerAppEnvironment.id
    containerImageName: containerImageName
    existingEnvVars: existingEnvVars
    existingSecretRefs: existingSecretRefs
    existingSecrets: existingSecrets
  }
}

output containerAppId string = containerAppModule.outputs.containerAppId
output containerAppName string = containerAppModule.outputs.containerAppName
output containerAppUrl string = containerAppModule.outputs.containerAppUrl
output storageAccountName string = storageAccount.name
output staticSiteName string = staticSite.name
output staticSiteUrl string = staticSite.properties.defaultHostname
output resourceGroupId string = resourceGroup().id
output resourceGroupName string = resourceGroup().name
output appName string = appName
