targetScope = 'resourceGroup'
provider microsoftGraph

param name string
param environment string
param staticSiteUrl string
param containerAppUrl string
param guidSalt string = '9f7046d3-4948-41cd-9c27-89d0c90186fa'
param uniqueDisplayName string = guid(resourceGroup().id, name, environment, 'entra-app-registration', guidSalt)

resource entraAppRegistration 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: uniqueDisplayName
  displayName: 'entra-app-${name}-${environment}-${uniqueDisplayName}'
  web: {
    redirectUris: [
     'https://localhost:54629/', 'https://${containerAppUrl}/'
    ]
  }
  spa: {
    redirectUris: [
      'https://localhost:54629/swagger/oauth2-redirect.html', 'http://localhost:8080/', 'https://${staticSiteUrl}/', 'https://${containerAppUrl}/swagger/oauth2-redirect.html'
    ]
  }
  requiredResourceAccess: [
    {
      resourceAppId: '00000003-0000-0000-c000-000000000000'
      resourceAccess: [
        {
          id: guid(uniqueDisplayName, 'Resource Access Scope') 
          type: 'Scope'
        }
      ]
    }
  ]
  api: {
    oauth2PermissionScopes: [
      {
        id: guid(uniqueDisplayName, 'Diary.Update Scope') 
        value: 'Diary.Update'
        adminConsentDisplayName: 'Update diary details'
        adminConsentDescription: 'Update diary details within the ccDiary API'
        isEnabled: true
        type: 'Admin'
      }
    ]
  }
}

resource entraAppRegistrationUpdate 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: uniqueDisplayName  
  displayName: 'entra-app-${name}-${environment}-${uniqueDisplayName}'
  identifierUris: [
    'api://${entraAppRegistration.appId}'
  ]
}

output entraApplicationIdURI string = entraAppRegistrationUpdate.identifierUris[0]
output entraClientId string = entraAppRegistrationUpdate.appId
output entraTenantId string = subscription().tenantId
