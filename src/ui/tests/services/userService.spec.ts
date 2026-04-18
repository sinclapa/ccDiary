import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getMe } from '@/services/modules/userService'
import type { AppUser } from '@/services/models/appUser'

const baseUrl = 'http://localhost'

describe('userService', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_API', baseUrl)
  })

  it('getMe returns user on success', async () => {
    const user: AppUser = {
      userId: 'u1',
      displayName: 'John',
      email: 'john@example.com',
      role: 'diary-admin',
      entraObjectId: 'oid-1',
    }
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      json: async () => user,
    } as Response)

    const result = await getMe()

    expect(globalThis.fetch).toHaveBeenCalledWith(new URL('v1/User/Me', baseUrl))
    expect(result).toEqual(user)
  })

  it('getMe returns null when response is not ok', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: false } as Response)

    const result = await getMe()

    expect(result).toBeNull()
  })
})
