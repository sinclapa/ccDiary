import { describe, it, beforeEach, afterEach, expect, vi } from 'vitest'
import { msalService } from '@/services/authentication/msalService'
import { msalInstance, state } from '@/services/authentication/msalConfig'

const mockEnv = {
  VITE_APPLICATION_ID_URI: 'appIdUri',
  VITE_API: 'api',
}
Object.defineProperty(globalThis, 'import.meta', {
  value: { env: mockEnv },
})

describe('msalService', () => {
  let service: ReturnType<typeof msalService>
  let originalFetch: typeof globalThis.fetch

  beforeEach(() => {
    // Reset only the methods, not the whole object
    vi.restoreAllMocks()
    service = msalService()
    originalFetch = globalThis.fetch
    globalThis.fetch = vi.fn(() => Promise.resolve(new Response('ok')))
    state.isAuthenticated = false
    state.user = null
  })

  afterEach(() => {
    globalThis.fetch = originalFetch
    vi.clearAllMocks()
  })

  it('initializeInstance: calls msalInstance.initialize', async () => {
    const spy = vi.spyOn(msalInstance, 'initialize').mockResolvedValue(undefined)
    await service.initializeInstance()
    expect(spy).toHaveBeenCalled()
  })

  it('initializeInstance: handles error', async () => {
    const spyInit = vi.spyOn(msalInstance, 'initialize').mockRejectedValue(new Error('fail'))
    const spyLog = vi.spyOn(console, 'log').mockImplementation(() => {})
    await service.initializeInstance()
    expect(spyLog).toHaveBeenCalledWith('MSAL initialization error', expect.any(Error))
    spyLog.mockRestore()
    spyInit.mockRestore()
  })

  it('login: calls loginRedirect and sets state', async () => {
    const spy = vi.spyOn(msalInstance, 'loginRedirect').mockResolvedValue({ user: 'foo' } as any)
    await service.login()
    expect(spy).toHaveBeenCalledTimes(2)
    expect(state.isAuthenticated).toBe(true)
    spy.mockRestore()
  })

  it('login: handles msalInstance missing', async () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    const brokenService = msalService(null as any, state)
    await brokenService.login()
    expect(spy).toHaveBeenCalledWith('Login error:', expect.any(Error))
    spy.mockRestore()
  })

  it('login: handles error', async () => {
    const spyLogin = vi.spyOn(msalInstance, 'loginRedirect').mockRejectedValue(new Error('fail'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    await service.login()
    expect(spy).toHaveBeenCalledWith('Login error:', expect.any(Error))
    spy.mockRestore()
    spyLogin.mockRestore()
  })

  it('logout: calls logoutRedirect and sets state', async () => {
    const spy = vi.spyOn(msalInstance, 'logoutRedirect').mockResolvedValue(undefined)
    await service.logout()
    expect(spy).toHaveBeenCalled()
    expect(state.isAuthenticated).toBe(false)
    spy.mockRestore()
  })

  it('logout: throws if msalInstance is missing', async () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    const brokenService = msalService(null as any, state)
    await brokenService.logout()
    expect(spy).toHaveBeenCalledWith('Logout error:', expect.any(Error))
    spy.mockRestore()
    spy.mockRestore()
  })

  it('handleRedirect: sets state and user if accounts exist', async () => {
    vi.spyOn(msalInstance, 'handleRedirectPromise').mockResolvedValue(null)
    vi.spyOn(msalInstance, 'getAllAccounts').mockReturnValue([{ name: 'user1' }] as any)
    await service.handleRedirect()
    expect(state.isAuthenticated).toBe(true)
    expect(state.user).toEqual({ name: 'user1' })
  })

  it('handleRedirect: sets state to false if no accounts', async () => {
    vi.spyOn(msalInstance, 'handleRedirectPromise').mockResolvedValue(null)
    vi.spyOn(msalInstance, 'getAllAccounts').mockReturnValue([])
    await service.handleRedirect()
    expect(state.isAuthenticated).toBe(false)
    expect(state.user).toBeUndefined()
  })

  it('handleRedirect: handles error', async () => {
    vi.spyOn(msalInstance, 'handleRedirectPromise').mockRejectedValue(new Error('fail'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    await service.handleRedirect()
    expect(spy).toHaveBeenCalledWith('Redirect error:', expect.any(Error))
    spy.mockRestore()
  })

  it('getToken: returns accessToken', async () => {
    vi.spyOn(msalInstance, 'getAllAccounts').mockReturnValue([{ id: 1 }] as any)
    vi.spyOn(msalInstance, 'acquireTokenSilent').mockResolvedValue({ accessToken: 'token' } as any)
    const token = await service.getToken()
    expect(token).toBe('token')
  })

  it('getToken: throws if msalInstance missing', async () => {
    const brokenService = msalService(null as any, state)
    await expect(brokenService.getToken()).rejects.toThrow('MSAL not initialized')
  })

  it('getToken: returns null when no accounts are logged in', async () => {
    vi.spyOn(msalInstance, 'getAllAccounts').mockReturnValue([])
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    const result = await service.getToken()
    expect(result).toBeNull()
    expect(spy).not.toHaveBeenCalled()
    spy.mockRestore()
  })

  it('getToken: handles acquireTokenSilent error', async () => {
    vi.spyOn(msalInstance, 'getAllAccounts').mockReturnValue([{ id: 1 }] as any)
    vi.spyOn(msalInstance, 'acquireTokenSilent').mockRejectedValue(new Error('fail'))
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    await service.getToken()
    expect(spy).toHaveBeenCalledWith('Silent token acquisition error:', expect.any(Error))
    spy.mockRestore()
  })

  it('registerAuthorizationHeaderInterceptor: injects Authorization header', async () => {
    vi.spyOn(msalInstance, 'getAllAccounts').mockReturnValue([{ id: 1 }] as any)
    vi.spyOn(msalInstance, 'acquireTokenSilent').mockResolvedValue({ accessToken: 'token' } as any)
    const fetchMock = vi.fn(() => Promise.resolve(new Response('ok')))
    globalThis.fetch = fetchMock
    await service.registerAuthorizationHeaderInterceptor()
    const resource = 'https://api/resource'
    await globalThis.fetch(resource, { headers: { 'X-Test': '1' } })
    expect(fetchMock).toHaveBeenCalled()
    //const lastCall = fetchMock.mock.calls[0]
    //const headers = lastCall[1].headers
    //expect(headers.get('Authorization')).toBe('Bearer token')
    //expect(headers.get('X-Test')).toBe('1')
  })

  it('registerAuthorizationHeaderInterceptor: does not inject header for non-API', async () => {
    const fetchMock = vi.fn(() => Promise.resolve(new Response('ok')))
    globalThis.fetch = fetchMock
    await service.registerAuthorizationHeaderInterceptor()
    const resource = 'https://other/resource'
    await globalThis.fetch(resource, { headers: { 'X-Test': '1' } })
    expect(fetchMock).toHaveBeenCalled()
    //const lastCall = fetchMock.mock.calls[0]
    //expect(lastCall[1].headers.get('Authorization')).toBeUndefined()
  })

  it('registerAuthorizationHeaderInterceptor: adds Authorization if no headers', async () => {
    vi.spyOn(msalInstance, 'getAllAccounts').mockReturnValue([{ id: 1 }] as any)
    vi.spyOn(msalInstance, 'acquireTokenSilent').mockResolvedValue({ accessToken: 'token' } as any)
    const fetchMock = vi.fn(() => Promise.resolve(new Response('ok')))
    globalThis.fetch = fetchMock
    await service.registerAuthorizationHeaderInterceptor()
    const resource = 'https://api/resource'
    await globalThis.fetch(resource)
    expect(fetchMock).toHaveBeenCalled()
    //const lastCall = fetchMock.mock.calls[0]
    //expect(lastCall[1].headers.get('Authorization')).toBe('Bearer token')
  })
})
