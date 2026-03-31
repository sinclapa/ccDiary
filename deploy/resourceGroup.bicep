targetScope = 'resourceGroup'

param name string
param environment string
param adminUser string
param adminUserSID string
param externalDomainName string?
param location string = resourceGroup().location
param containerImageName string
var appName string = '${name}-${environment}'
var sqlServerName string = 'sql-${appName}'

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

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
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
  parent: sqlServer
  name: 'sql-fw-${appName}-allow-azure-services'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: 'sqldb-${appName}'
  location: location
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
  }
  properties: {
    createMode: 'Default'
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32 GB
    zoneRedundant: false
    availabilityZone: 'NoPreference'
    autoPauseDelay: 60
    readScale: 'Disabled'
    minCapacity: json('0.5')
    requestedBackupStorageRedundancy: 'Local'
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    isLedgerOn: false
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    maintenanceConfigurationId: subscriptionResourceId('Microsoft.Maintenance/publicMaintenanceConfigurations', 'SQL_WestEurope_DB_2')
  }
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
  }
}

output containerAppId string = containerAppModule.outputs.containerAppId
output containerAppName string = containerAppModule.outputs.containerAppName
output containerAppUrl string = containerAppModule.outputs.containerAppUrl
output databaseServer string = sqlServer.properties.fullyQualifiedDomainName
output databaseServerName string = sqlServer.name
output databaseId string = sqlDatabase.id
output databaseName string = sqlDatabase.name
output staticSiteName string = staticSite.name
output staticSiteUrl string = staticSite.properties.defaultHostname
output resourceGroupId string = resourceGroup().id
output resourceGroupName string = resourceGroup().name
output appName string = appName
