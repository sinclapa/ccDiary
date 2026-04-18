import { beforeEach, describe, expect, it, vi } from 'vitest'
import { approveRequest, declineRequest, getPendingRequests } from '@/services/modules/adminService'
import type { AccessRequest } from '@/services/models/accessRequest'

const baseUrl = 'http://localhost'

const mockRequest: AccessRequest = {
  accessRequestId: 'req-1',
  displayName: 'John Doe',
  email: 'john@example.com',
  status: 'pending',
  requestedAt: '2024-01-15T00:00:00Z',
}

describe('adminService', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_API', baseUrl)
  })

  describe('getPendingRequests', () => {
    it('returns requests from the API on success', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue({
        ok: true,
        json: async () => [mockRequest],
      } as Response)

      const result = await getPendingRequests()

      expect(globalThis.fetch).toHaveBeenCalledWith(new URL('v1/Admin/Requests', baseUrl))
      expect(result).toEqual([mockRequest])
    })

    it('returns empty array when response is not ok', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: false } as Response)

      const result = await getPendingRequests()

      expect(result).toEqual([])
    })
  })

  describe('approveRequest', () => {
    it('returns ok true and redeemUrl on success', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue({
        ok: true,
        json: async () => ({ redeemUrl: 'https://example.com/invite/xyz' }),
      } as Response)

      const result = await approveRequest('req-1')

      expect(globalThis.fetch).toHaveBeenCalledWith(
        new URL('v1/Admin/Approve/req-1', baseUrl),
        { method: 'PUT' }
      )
      expect(result).toEqual({ ok: true, redeemUrl: 'https://example.com/invite/xyz' })
    })

    it('returns ok true and null redeemUrl when redeemUrl not in response', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue({
        ok: true,
        json: async () => ({}),
      } as Response)

      const result = await approveRequest('req-2')

      expect(result).toEqual({ ok: true, redeemUrl: null })
    })

    it('returns ok false when response is not ok', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: false } as Response)

      const result = await approveRequest('req-3')

      expect(result).toEqual({ ok: false, redeemUrl: null })
    })
  })

  describe('declineRequest', () => {
    it('returns true on success', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: true } as Response)

      const result = await declineRequest('req-1')

      expect(globalThis.fetch).toHaveBeenCalledWith(
        new URL('v1/Admin/Decline/req-1', baseUrl),
        { method: 'PUT' }
      )
      expect(result).toBe(true)
    })

    it('returns false when response is not ok', async () => {
      vi.spyOn(globalThis, 'fetch').mockResolvedValue({ ok: false } as Response)

      const result = await declineRequest('req-2')

      expect(result).toBe(false)
    })
  })
})
