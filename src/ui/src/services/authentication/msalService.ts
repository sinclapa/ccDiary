import { msalInstance, state } from '@/services/authentication/msalConfig'

export function msalService() {
    const initialize = async () => {
      try {
        await msalInstance.initialize() // Call the initialize function
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
        if (!msalInstance) {
            throw new Error('MSAL not initialized. Call initializeMsal() before using MSAL API.')
        }

        await msalInstance.logoutRedirect()
        state.isAuthenticated = false
        console.log('Logged out')
    }

    const handleRedirect = async () => {
        try {
            await msalInstance.handleRedirectPromise()
            state.isAuthenticated = msalInstance.getAllAccounts().length > 0
            state.user = msalInstance.getAllAccounts()[0]
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
                scopes: [`${import.meta.env.VITE_APPLICATIONID_URI}/Diary.Update`],
                account: accounts[0]
            }
            const silentResponse = await msalInstance.acquireTokenSilent(silentRequest)
            return silentResponse.accessToken
        } catch (error) {
            console.error('Silent token acquisition error:', error)
        }
    }

    const { fetch: originalFetch } = window;
    const registerAuthorizationHeaderInterceptor = async () => {
        window.fetch = async (...args) => {
            let [resource, options] = args;
            if (resource.toString().includes(import.meta.env.VITE_API)) {
                const accessToken = await getToken()
                if (options === undefined) {
                    options = { headers: {} }
                }
                let headers = new Headers(options.headers)
                if (headers.has('Authorization')) {
                    headers.set('Authorization', `Bearer ${accessToken}`)
                }
                else {
                    headers.append('Authorization', `Bearer ${accessToken}`);
                }
                options.headers = headers;
            }
            const response = await originalFetch(resource, options);
            return response;
        }
    }

    return { initialize, login, logout, handleRedirect, registerAuthorizationHeaderInterceptor }
}
