import { beforeEach, describe, expect, it, vi } from 'vitest'
import { submitAccessRequest } from '@/services/modules/accessRequestService'

const baseUrl = 'http://localhost'

describe('accessRequestService', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_API', baseUrl)
  })

  it('submitAccessRequest sends a POST request with display name and email', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: true } as Response)

    await submitAccessRequest('John Doe', 'john@example.com')

    expect(fetchSpy).toHaveBeenCalledWith(
      new URL('v1/AccessRequest/Submit', baseUrl),
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName: 'John Doe', email: 'john@example.com' }),
      }
    )
  })

  it('submitAccessRequest throws with message from JSON body on failure', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      json: async () => ({ message: 'Email already registered' }),
    } as Response)

    await expect(submitAccessRequest('Jane', 'jane@example.com')).rejects.toThrow('Email already registered')
  })

  it('submitAccessRequest throws generic message when error body cannot be parsed', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      json: async () => { throw new Error('not json') },
    } as Response)

    await expect(submitAccessRequest('Jane', 'jane@example.com')).rejects.toThrow('Failed to submit access request')
  })
})
