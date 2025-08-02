import { describe, it, beforeEach, afterEach, expect, vi } from 'vitest'
import { msalService } from '@/services/authentication/msalService'
import { msalInstance, state } from '@/services/authentication/msalConfig'

vi.mock('@/services/authentication/msalConfig', () => ({
  msalInstance: {
    initialize: vi.fn(),
    loginRedirect: vi.fn(),
    logoutRedirect: vi.fn(),
    handleRedirectPromise: vi.fn(),
    getAllAccounts: vi.fn(),
    acquireTokenSilent: vi.fn(),
  },
  state: {
    isAuthenticated: false,
    user: null,
  },
}))

const mockEnv = {
  VITE_APPLICATIONID_URI: 'appIdUri',
  VITE_API: 'api',
}
Object.defineProperty(global, 'import.meta', {
  value: { env: mockEnv },
})


describe('msalService', () => {
  let service: ReturnType<typeof msalService>
  let originalFetch: typeof window.fetch

  beforeEach(() => {
    service = msalService()
    originalFetch = window.fetch
    window.fetch = vi.fn(() => Promise.resolve(new Response('ok')))
    Object.assign(msalInstance, {
      initialize: vi.fn(),
      loginRedirect: vi.fn(),
      logoutRedirect: vi.fn(),
      handleRedirectPromise: vi.fn(),
      getAllAccounts: vi.fn(),
      acquireTokenSilent: vi.fn(),
    })
    state.isAuthenticated = false
    state.user = null
  })

  afterEach(() => {
    window.fetch = originalFetch
    vi.clearAllMocks()
  })

  it('initializeInstance: calls msalInstance.initialize', async () => {
    await service.initializeInstance()
    expect(msalInstance.initialize).toHaveBeenCalled()
  })

  it('initializeInstance: handles error', async () => {
    (msalInstance.initialize as any).mockRejectedValueOnce(new Error('fail'))
    const spy = vi.spyOn(console, 'log').mockImplementation(() => {})
    await service.initializeInstance()
    expect(spy).toHaveBeenCalledWith('MSAL initialization error', expect.any(Error))
    spy.mockRestore()
  })

  it('login: calls loginRedirect and sets state', async () => {
    (msalInstance.loginRedirect as any).mockResolvedValueOnce({ user: 'foo' })
    await service.login()
    expect(msalInstance.loginRedirect).toHaveBeenCalledTimes(2)
    expect(state.isAuthenticated).toBe(true)
  })

/*   it('login: handles msalInstance missing', async () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    // Dynamically mock the module for this test
    vi.doMock('@/services/authentication/msalConfig', () => ({
      msalInstance: undefined,
      state: {
        isAuthenticated: false,
        user: null,
      },
    }))
    // Import msalService after mocking
    const { msalService: brokenMsalService } = await import('@/services/authentication/msalService')
    const serviceWithMissingMsal = brokenMsalService()
    await serviceWithMissingMsal.login()
    expect(spy).toHaveBeenCalledWith('Login error:', expect.any(Error))
    spy.mockRestore()
    vi.resetModules() // Clean up for other tests
  }) */

  it('login: handles error', async () => {
    (msalInstance.loginRedirect as any).mockRejectedValueOnce(new Error('fail'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    await service.login()

    expect(spy).toHaveBeenCalledWith('Login error:', expect.any(Error))
    spy.mockRestore()
  })

  it('logout: calls logoutRedirect and sets state', async () => {
    await service.logout()
    expect(msalInstance.logoutRedirect).toHaveBeenCalled()
    expect(state.isAuthenticated).toBe(false)
  })
/*
  it('logout: handles msalInstance missing', async () => {
    const orig = msalInstance
    require('@/services/authentication/msalConfig').msalInstance = null
    await expect(service.logout()).rejects.toThrow('MSAL not initialized')
    require('@/services/authentication/msalConfig').msalInstance = orig
  })

  it('handleRedirect: sets state and user if accounts exist', async () => {
    (msalInstance.handleRedirectPromise as any).mockResolvedValueOnce(undefined)
    (msalInstance.getAllAccounts as any).mockReturnValue([{ name: 'user1' }])
    await service.handleRedirect()
    expect(state.isAuthenticated).toBe(true)
    expect(state.user).toEqual({ name: 'user1' })
  })

  it('handleRedirect: sets state to false if no accounts', async () => {
    (msalInstance.handleRedirectPromise as any).mockResolvedValueOnce(undefined)
    (msalInstance.getAllAccounts as any).mockReturnValue([])
    await service.handleRedirect()
    expect(state.isAuthenticated).toBe(false)
    expect(state.user).toBeUndefined()
  })
 */
  it('handleRedirect: handles error', async () => {
    (msalInstance.handleRedirectPromise as any).mockRejectedValueOnce(new Error('fail'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    await service.handleRedirect()
    expect(spy).toHaveBeenCalledWith('Redirect error:', expect.any(Error))
    spy.mockRestore()
  })
/*
  it('getToken: returns accessToken', async () => {
    (msalInstance.getAllAccounts as any).mockReturnValue([{ id: 1 }])
    (msalInstance.acquireTokenSilent as any).mockResolvedValueOnce({ accessToken: 'token' })
    // @ts-ignore
    const token = await service['getToken']()
    expect(token).toBe('token')
  })

  it('getToken: throws if msalInstance missing', async () => {
    const orig = msalInstance
    // @ts-ignore
    require('@/services/authentication/msalConfig').msalInstance = null
    // @ts-ignore
    await expect(service['getToken']()).rejects.toThrow('MSAL not initialized')
    // @ts-ignore
    require('@/services/authentication/msalConfig').msalInstance = orig
  })

  it('getToken: throws if no accounts', async () => {
    (msalInstance.getAllAccounts as any).mockReturnValue([])
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    // @ts-ignore
    await service['getToken']()
    expect(spy).toHaveBeenCalledWith('Silent token acquisition error:', expect.any(Error))
    spy.mockRestore()
  })

  it('getToken: handles acquireTokenSilent error', async () => {
    (msalInstance.getAllAccounts as any).mockReturnValue([{ id: 1 }])
    (msalInstance.acquireTokenSilent as any).mockRejectedValueOnce(new Error('fail'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    // @ts-ignore
    await service['getToken']()
    expect(spy).toHaveBeenCalledWith('Silent token acquisition error:', expect.any(Error))
    spy.mockRestore()
  })

  it('registerAuthorizationHeaderInterceptor: injects Authorization header', async () => {
    (msalInstance.getAllAccounts as any).mockReturnValue([{ id: 1 }])
    (msalInstance.acquireTokenSilent as any).mockResolvedValueOnce({ accessToken: 'token' })
    await service.registerAuthorizationHeaderInterceptor()
    const resource = 'https://api/resource'
    await window.fetch(resource, { headers: { 'X-Test': '1' } })
    const lastCall = (window.fetch as any).mock.calls[0]
    const headers = lastCall[1].headers
    expect(headers.get('Authorization')).toBe('Bearer token')
    expect(headers.get('X-Test')).toBe('1')
  })

  it('registerAuthorizationHeaderInterceptor: does not inject header for non-API', async () => {
    await service.registerAuthorizationHeaderInterceptor()
    const resource = 'https://other/resource'
    await window.fetch(resource, { headers: { 'X-Test': '1' } })
    const lastCall = (window.fetch as any).mock.calls[0]
    expect(lastCall[1].headers.get('Authorization')).toBeUndefined()
  })

  it('registerAuthorizationHeaderInterceptor: adds Authorization if no headers', async () => {
    (msalInstance.getAllAccounts as any).mockReturnValue([{ id: 1 }])
    (msalInstance.acquireTokenSilent as any).mockResolvedValueOnce({ accessToken: 'token' })
    await service.registerAuthorizationHeaderInterceptor()
    const resource = 'https://api/resource'
    await window.fetch(resource)
    const lastCall = (window.fetch as any).mock.calls[0]
    expect(lastCall[1].headers.get('Authorization')).toBe('Bearer token')
  }) */
})
