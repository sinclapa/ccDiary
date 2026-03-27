import { setActivePinia, createPinia } from 'pinia'
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { useApiStatusStore } from '@/stores/apiStatus'

vi.mock('@/utils/appConfig', () => ({
  getAppConfigField: () => 'https://api.example.com/',
}))

describe('useApiStatusStore', () => {
  let store: ReturnType<typeof useApiStatusStore>

  beforeEach(() => {
    setActivePinia(createPinia())
    store = useApiStatusStore()
    vi.useFakeTimers()
  })

  afterEach(() => {
    store.stopPolling()
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('has correct default state', () => {
    expect(store.available).toBe(true)
    expect(store.checking).toBe(false)
    expect(store.pollTimer).toBeNull()
    expect(store.recoveryCount).toBe(0)
  })

  it('sets available to true on successful health check', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }))
    store.available = false

    await store.checkHealth()

    expect(store.available).toBe(true)
    expect(store.checking).toBe(false)
  })

  it('sets available to false on failed health check', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false }))

    await store.checkHealth()

    expect(store.available).toBe(false)
  })

  it('sets available to false on network error', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    await store.checkHealth()

    expect(store.available).toBe(false)
  })

  it('starts polling when API becomes unavailable', () => {
    store.setAvailable(false)

    expect(store.pollTimer).not.toBeNull()
  })

  it('stops polling when API becomes available', () => {
    store.setAvailable(false)
    expect(store.pollTimer).not.toBeNull()

    store.setAvailable(true)
    expect(store.pollTimer).toBeNull()
  })

  it('increments recoveryCount when API recovers from unavailable', () => {
    store.setAvailable(false)
    store.setAvailable(true)

    expect(store.recoveryCount).toBe(1)
  })

  it('does not increment recoveryCount when API was already available', () => {
    store.setAvailable(true)

    expect(store.recoveryCount).toBe(0)
  })

  it('does not create duplicate poll timers', () => {
    store.setAvailable(false)
    const firstTimer = store.pollTimer

    store.startPolling()
    expect(store.pollTimer).toBe(firstTimer)
  })

  it('polls periodically when unavailable', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({ ok: false })
      .mockResolvedValueOnce({ ok: true })
    vi.stubGlobal('fetch', fetchMock)

    await store.checkHealth()
    expect(store.available).toBe(false)
    expect(fetchMock).toHaveBeenCalledTimes(1)

    await vi.advanceTimersByTimeAsync(5000)
    expect(fetchMock).toHaveBeenCalledTimes(2)
    expect(store.available).toBe(true)
    expect(store.pollTimer).toBeNull()
  })

  describe('registerFetchInterceptor', () => {
    it('sets available to false when API fetch throws', async () => {
      const originalFetch = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))
      vi.stubGlobal('fetch', originalFetch)

      store.registerFetchInterceptor()

      await expect(
        globalThis.fetch('https://api.example.com/v1/Diary/Get')
      ).rejects.toThrow()

      expect(store.available).toBe(false)
    })

    it('does not change availability when API fetch succeeds', async () => {
      const originalFetch = vi.fn().mockResolvedValue({ ok: true })
      vi.stubGlobal('fetch', originalFetch)

      store.available = false
      store.registerFetchInterceptor()

      await globalThis.fetch('https://api.example.com/v1/Diary/Get')

      expect(store.available).toBe(false)
    })

    it('does not affect non-API fetches', async () => {
      const originalFetch = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))
      vi.stubGlobal('fetch', originalFetch)

      store.registerFetchInterceptor()

      await expect(
        globalThis.fetch('https://other.example.com/data')
      ).rejects.toThrow()

      expect(store.available).toBe(true)
    })

    it('only registers interceptor once', () => {
      vi.stubGlobal('fetch', vi.fn())
      store.registerFetchInterceptor()
      const fetchAfterFirst = globalThis.fetch

      store.registerFetchInterceptor()
      expect(globalThis.fetch).toBe(fetchAfterFirst)
    })
  })
})
