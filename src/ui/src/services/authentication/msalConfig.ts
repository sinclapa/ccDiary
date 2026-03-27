import { getAppConfigField } from '@/utils/appConfig'
import { type AccountInfo, PublicClientApplication, type RedirectRequest } from '@azure/msal-browser'
import { reactive } from 'vue'

export const msalConfig = {
  auth: {
    clientId: getAppConfigField('VITE_CLIENT_ID'),
    authority: 'https://login.microsoftonline.com/' + getAppConfigField('VITE_TENANT_ID'),
    redirectUri: globalThis.location.origin,
    postLogoutRedirectUri: globalThis.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
}

export const graphScopes: RedirectRequest = {
  scopes: ['openid', 'profile'],
}

export const state = reactive({
  isAuthenticated: false,
  user: null as AccountInfo | null,
})

export const msalInstance = new PublicClientApplication(msalConfig)
