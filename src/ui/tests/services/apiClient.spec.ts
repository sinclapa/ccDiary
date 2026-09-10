import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  apiFetch,
  apiFetchJson,
  ApiUnavailableError,
  apiUrl,
  isUnavailable,
} from '@/services/modules/apiClient'

vi.mock('@/utils/appConfig', () => ({
  getAppConfigField: () => 'https://api.example.com/',
}))

const ok = (body: unknown = {}) => ({ ok: true, status: 200, json: async () => body })
const gateway = (status: number) => ({ ok: false, status, json: async () => ({}) })

describe('apiClient', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  /**
   * Runs a call to completion, letting the backoff timers fire without real waiting.
   *
   * The outcome is captured synchronously. Advancing the timers first would leave a
   * rejection unobserved for a microtask, which the runner reports as an unhandled one.
   */
  async function withTimers<T> (run: () => Promise<T>): Promise<T> {
    const settled = run().then(
      value => () => value,
      error => () => { throw error },
    )
    await vi.runAllTimersAsync()
    return (await settled)()
  }

  it('builds URLs relative to the configured API base', () => {
    expect(apiUrl('v1/Diary/Get').toString()).toBe('https://api.example.com/v1/Diary/Get')
  })

  it('identifies gateway responses as the API being unavailable', () => {
    expect(isUnavailable({ status: 503 } as Response)).toBe(true)
    expect(isUnavailable({ status: 404 } as Response)).toBe(false)
  })

  it('carries the status on ApiUnavailableError', () => {
    const error = new ApiUnavailableError(503)
    expect(error.status).toBe(503)
    expect(error.name).toBe('ApiUnavailableError')
    expect(error.message).toContain('503')
  })

  it('returns a successful response without retrying', async () => {
    const fetchMock = vi.fn().mockResolvedValue(ok())
    vi.stubGlobal('fetch', fetchMock)

    await withTimers(() => apiFetch(apiUrl('v1/Diary/Get')))

    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('does not retry a response the API actually produced', async () => {
    const fetchMock = vi.fn().mockResolvedValue({ ok: false, status: 404, json: async () => ({}) })
    vi.stubGlobal('fetch', fetchMock)

    const response = await withTimers(() => apiFetch(apiUrl('v1/Diary/Get')))

    expect(response.status).toBe(404)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it.each([502, 503, 504])('retries a %i until the container answers', async status => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(gateway(status))
      .mockResolvedValueOnce(ok())
    vi.stubGlobal('fetch', fetchMock)

    const response = await withTimers(() => apiFetch(apiUrl('v1/Diary/Get')))

    expect(response.ok).toBe(true)
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('gives up after the final attempt and returns the last gateway response', async () => {
    const fetchMock = vi.fn().mockResolvedValue(gateway(503))
    vi.stubGlobal('fetch', fetchMock)

    const response = await withTimers(() => apiFetch(apiUrl('v1/Diary/Get')))

    expect(response.status).toBe(503)
    expect(fetchMock).toHaveBeenCalledTimes(4)
  })

  it('retries a thrown error for an idempotent request', async () => {
    const fetchMock = vi.fn()
      .mockRejectedValueOnce(new TypeError('Failed to fetch'))
      .mockResolvedValueOnce(ok())
    vi.stubGlobal('fetch', fetchMock)

    const response = await withTimers(() => apiFetch(apiUrl('v1/Diary/Get')))

    expect(response.ok).toBe(true)
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('does not retry a thrown error for a write, which may have been applied', async () => {
    const fetchMock = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))
    vi.stubGlobal('fetch', fetchMock)

    await expect(withTimers(() => apiFetch(apiUrl('v1/Diary/Create'), { method: 'POST' })))
      .rejects.toThrow('Failed to fetch')
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })

  it('retries a write on a gateway error, which never reached the app', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(gateway(503))
      .mockResolvedValueOnce(ok())
    vi.stubGlobal('fetch', fetchMock)

    const response = await withTimers(() =>
      apiFetch(apiUrl('v1/Diary/Create'), { method: 'POST' }))

    expect(response.ok).toBe(true)
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })

  it('rethrows once retries are exhausted', async () => {
    const fetchMock = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'))
    vi.stubGlobal('fetch', fetchMock)

    await expect(withTimers(() => apiFetch(apiUrl('v1/Diary/Get'))))
      .rejects.toThrow('Failed to fetch')
    expect(fetchMock).toHaveBeenCalledTimes(4)
  })

  it('parses JSON on success', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(ok({ title: 'Trip' })))

    const result = await withTimers(() =>
      apiFetchJson(apiUrl('v1/Diary/Get'), { title: 'fallback' }))

    expect(result).toEqual({ title: 'Trip' })
  })

  it('returns the fallback rather than parsing an error body', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      json: async () => { throw new SyntaxError('Unexpected token <') },
    }))

    const result = await withTimers(() =>
      apiFetchJson(apiUrl('v1/Diary/Get'), { items: [] }))

    expect(result).toEqual({ items: [] })
  })
})
