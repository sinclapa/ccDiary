targetScope = 'resourceGroup'

param name string
param environment string
param adminUser string
param adminUserSID string
param location string = resourceGroup().location
param imageName string
param isContainerImagePresent bool = false

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
    minCapacity: json('0.5')     
    requestedBackupStorageRedundancy: 'Local'
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    isLedgerOn: false
    //useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
  }
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1         
  } 
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

module containerAppModule 'containerApps.bicep' = if (isContainerImagePresent){
  name: 'containerApps'
  params: {
    name: name
    environment: environment
    imageName: imageName
    location: location
    containerAppsEnvironmentId: containerAppEnvironment.id
    containerRegistryLoginServer: containerRegistry.properties.loginServer
    containerRegistryName: containerRegistry.name
    containerRegistryPassword: containerRegistry.listCredentials().passwords[0].value
  }
}

output containerAppName string = (isContainerImagePresent) ? containerAppModule.outputs.containerAppsName : ''
output containerRegistryName string = containerRegistry.name
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output databaseServer string = databaseServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
