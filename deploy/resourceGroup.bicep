targetScope = 'resourceGroup'

param name string
param environment string
param adminUser string
param adminUserSID string
param location string = resourceGroup().location
var apiImageName = 'ccdiaryapi'
var userAssignedIdentityName = 'configDeployer'
var roleAssignmentName = guid(resourceGroup().id, 'contributor')
var contributorRoleDefinitionId = resourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: userAssignedIdentityName
  location: resourceGroup().location
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'acr${name}${uniqueString(resourceGroup().id)}'
  location: location
  properties: {
    adminUserEnabled: true
  }
  sku: {
    name: 'Basic'
  }
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2020-04-01-preview' = {
  name: roleAssignmentName
  properties: {
    roleDefinitionId: contributorRoleDefinitionId
    principalId: userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

resource ccdiaryApiImageExists 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'ccdiaryApiImageExists'  
  kind: 'AzurePowerShell'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  location: location
  properties: {
    retentionInterval: 'PT1H'
    azPowerShellVersion: '12.2'
    cleanupPreference: 'OnSuccess'
    arguments: '-AcrName ${containerRegistry.name} -ImageName ${apiImageName}'
    scriptContent: '''
      param (
        [string] $AcrName,
        [string] $ImageName
      )
      $acrList = Get-AzContainerRegistryRepository -RegistryName $AcrName
      $exists = $acrList -contains $ImageName
      $DeploymentScriptOutputs = @{}
      $DeploymentScriptOutputs['Exists'] = $exists
    '''
  }
  dependsOn: [
    roleAssignment
  ]
}

@description('This module seeds the ACR with the public version of the app')
module acrImportImage 'br/public:deployment-scripts/import-acr:3.0.1' = {
  name: 'importContainerImage'
  params: {
    acrName: containerRegistry.name
    location: location
    images: array(ccdiaryApiImageExists.properties.outputs.Exists ? '${containerRegistry.properties.loginServer}/${apiImageName}:latest' : 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest')
  }
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'logs-${name}-${environment}'
  location: location
  properties: {
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: -1
    }
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: 'env-${name}-${environment}'
  location: location
  properties: {
    zoneRedundant: false
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: 'null'
        dynamicJsonColumns: false
      }
    }    
    workloadProfiles: [
      {
        workloadProfileType: 'Consumption'
        name: 'Consumption'
      }
    ]
  }
}

resource databaseServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'mssql-${name}-${environment}'
  location: location
  identity: {
    type: 'SystemAssigned'    
  }
  properties: {
    publicNetworkAccess: 'Enabled'    
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: adminUser
      sid: adminUserSID      
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }    
}

resource databaseServerFirewall 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: databaseServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }

}

resource database 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  name: '${name}-${environment}'
  location: location
  parent: databaseServer
  properties: {
    createMode: 'Default'      
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 1073741824
    zoneRedundant: false      
    availabilityZone: 'NoPreference'
    autoPauseDelay: 60
    readScale: 'Disabled' 
    //minCapacity: json('0.5')     
    requestedBackupStorageRedundancy: 'Local'
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    isLedgerOn: false
    //useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  /*sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1         
  } */
}

resource staticSite 'Microsoft.Web/staticSites@2023-01-01' = {
  name: 'site-${name}-${environment}'
  location: location
  sku: {
    name: 'free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
    provider: 'None'
    enterpriseGradeCdnStatus: 'Disabled'
  }  
}

module containerAppModule 'containerApps.bicep' = {
  name: 'containerApps'
  params: {
    name: name
    environment: environment
    location: location
    containerAppsEnvironmentId: containerAppEnvironment.id
    containerRegistryLoginServer: containerRegistry.properties.loginServer
    containerRegistryName: containerRegistry.name
    containerRegistryPassword: containerRegistry.listCredentials().passwords[0].value
    imageName: acrImportImage.outputs.importedImages[0].acrHostedImage
  }
}

module entraAppModule 'entraApp.bicep' = {
  name: 'entraApp'
  params: {
    name: name
    environment: environment
    staticSiteUrl: staticSite.properties.defaultHostname
    containerAppUrl: containerAppModule.outputs.containerAppUrl
  }
}

output containerAppName string = containerAppModule.outputs.containerAppName
output containerAppUrl string = containerAppModule.outputs.containerAppUrl
output containerRegistryName string = containerRegistry.name
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output databaseServer string = databaseServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
output staticSiteName string = staticSite.name
output staticSiteUrl string = staticSite.properties.defaultHostname
output entraApplicationIdURI string = entraAppModule.outputs.entraApplicationIdURI
output entraClientId string = entraAppModule.outputs.entraClientId
output entraTenantId string = entraAppModule.outputs.entraTenantId
