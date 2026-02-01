import { msalInstance as defaultMsalInstance, state as defaultState } from '@/services/authentication/msalConfig'
import { getAppConfigField } from '@/utils/appConfig'

export function msalService(
  msalInstance = defaultMsalInstance,
  state = defaultState,
  win: Window & typeof globalThis = window
) {
  const initializeInstance = async () => {
    try {
      await msalInstance.initialize()
    } catch (error) {
      console.log('MSAL initialization error', error)
    }
  }

  const login = async () => {
    try {
      if (!msalInstance) {
        throw new Error('MSAL not initialized. Call initializeMsal() before using MSAL API.')
      }
      await msalInstance.loginRedirect()
      state.isAuthenticated = true

      const loginResponse = await msalInstance.loginRedirect()
      state.isAuthenticated = true
      console.log('Login success:', loginResponse)
    } catch (error) {
      console.error('Login error:', error)
    }
  }

  const logout = async () => {
    try {
      if (!msalInstance) {
        throw new Error('MSAL not initialized. Call initializeMsal() before using MSAL API.')
      }

      await msalInstance.logoutRedirect()
      state.isAuthenticated = false
      console.log('Logged out')
    } catch (error) {
      console.error('Logout error:', error)
    }
  }

  const handleRedirect = async () => {
    try {
      await msalInstance.handleRedirectPromise()
      if (msalInstance.getAllAccounts()) {
        state.isAuthenticated = msalInstance.getAllAccounts().length > 0
        state.user = msalInstance.getAllAccounts()[0]
      }
    } catch (error) {
      console.error('Redirect error:', error)
    }
  }

  const getToken = async () => {
    if (!msalInstance) {
      throw new Error('MSAL not initialized. Call initializeMsal() before using MSAL API.')
    }
    try {
      const accounts = msalInstance.getAllAccounts()
      if (accounts.length === 0) {
        throw new Error('No accounts found. Please login first.')
      }
      const silentRequest = {
        scopes: [`${getAppConfigField('VITE_APPLICATION_ID_URI')}/Diary.Update`],
        account: accounts[0],
      }
      const silentResponse = await msalInstance.acquireTokenSilent(silentRequest)
      return silentResponse.accessToken
    } catch (error) {
      console.error('Silent token acquisition error:', error)
    }
  }

  const registerAuthorizationHeaderInterceptor = async () => {
    const originalFetch = win.fetch // capture at call time, not module load time
    win.fetch = async (...args) => {
      let [resource, options] = args
      if (resource.toString().includes(getAppConfigField('VITE_API'))) {
        const accessToken = await getToken()
        if (options === undefined) {
          options = { headers: {} }
        }
        const headers = new Headers(options.headers)
        if (headers.has('Authorization')) {
          headers.set('Authorization', `Bearer ${accessToken}`)
        } else {
          headers.append('Authorization', `Bearer ${accessToken}`)
        }
        options.headers = headers
      }
      const response = await originalFetch(resource, options)
      return response
    }
  }

  return { initializeInstance, login, logout, handleRedirect, getToken, registerAuthorizationHeaderInterceptor }
}
