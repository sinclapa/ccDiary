import { BrowserAuthError } from '@azure/msal-browser'
import { msalInstance as defaultMsalInstance, state as defaultState } from '@/services/authentication/msalConfig'
import { getAppConfigField } from '@/utils/appConfig'

export function msalService (
  msalInstance = defaultMsalInstance,
  state = defaultState,
  win: Window & typeof globalThis = globalThis as Window & typeof globalThis
) {
  const initializeInstance = async () => {
    try {
      await msalInstance.initialize()
    } catch (error) {
      console.log('MSAL initialization error', error)
    }
  }

  const login = async () => {
    if (!msalInstance) {
      console.error('Login error:', new Error('MSAL not initialized. Call initializeMsal() before using MSAL API.'))
      return
    }
    const scopes = [`${getAppConfigField('VITE_APPLICATION_ID_URI')}/Diary.Update`]
    try {
      const loginResponse = await msalInstance.loginPopup({ scopes })
      if (loginResponse) {
        state.isAuthenticated = true
        state.user = msalInstance.getAllAccounts()[0]
      }
    } catch (popupError) {
      if (popupError instanceof BrowserAuthError &&
        (popupError.errorCode === 'popup_window_error' || popupError.errorCode === 'empty_window_error')) {
        try {
          await msalInstance.loginRedirect({ scopes, state: win.location.pathname + win.location.search })
        } catch (error) {
          console.error('Login error:', error)
        }
      } else {
        console.error('Login error:', popupError)
      }
    }
  }

  const logout = async () => {
    try {
      if (!msalInstance) {
        throw new Error('MSAL not initialized. Call initializeMsal() before using MSAL API.')
      }

      await msalInstance.logoutRedirect({
        onRedirectNavigate: () => false,
      })
      state.isAuthenticated = false
      console.log('Logged out')
    } catch (error) {
      console.error('Logout error:', error)
    }
  }

  const handleRedirect = async (): Promise<string | null> => {
    try {
      const result = await msalInstance.handleRedirectPromise()
      if (msalInstance.getAllAccounts()) {
        state.isAuthenticated = msalInstance.getAllAccounts().length > 0
        state.user = msalInstance.getAllAccounts()[0]
      }
      return result?.state ?? null
    } catch (error) {
      console.error('Redirect error:', error)
      return null
    }
  }

  const getToken = async (): Promise<string | null> => {
    if (!msalInstance) {
      throw new Error('MSAL not initialized. Call initializeMsal() before using MSAL API.')
    }
    const accounts = msalInstance.getAllAccounts()
    if (accounts.length === 0) {
      return null
    }
    try {
      const silentRequest = {
        scopes: [`${getAppConfigField('VITE_APPLICATION_ID_URI')}/Diary.Update`],
        account: accounts[0],
      }
      const silentResponse = await msalInstance.acquireTokenSilent(silentRequest)
      return silentResponse.accessToken
    } catch (error) {
      console.error('Silent token acquisition error:', error)
      return null
    }
  }

  const registerAuthorizationHeaderInterceptor = async () => {
    const originalFetch = win.fetch // capture at call time, not module load time
    win.fetch = async (...args) => {
      let [resource, options] = args
      const resourceUrl = resource instanceof Request ? resource.url : resource.toString()
      if (resourceUrl.includes(getAppConfigField('VITE_API'))) {
        const accessToken = await getToken()
        if (accessToken) {
          const headers = new Headers(resource instanceof Request ? resource.headers : undefined)
          if (options?.headers) {
            new Headers(options.headers).forEach((value, key) => headers.set(key, value))
          }

          if (headers.has('Authorization')) {
            headers.set('Authorization', `Bearer ${accessToken}`)
          } else {
            headers.append('Authorization', `Bearer ${accessToken}`)
          }

          options = {
            ...options,
            headers,
          }
        }
      }
      const response = await originalFetch(resource, options)
      return response
    }
  }

  return { initializeInstance, login, logout, handleRedirect, getToken, registerAuthorizationHeaderInterceptor }
}
