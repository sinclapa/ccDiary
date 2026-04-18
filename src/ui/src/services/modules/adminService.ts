import type { AccessRequest } from '@/services/models/accessRequest'
import { getAppConfigField } from '@/utils/appConfig'

export async function getPendingRequests (): Promise<AccessRequest[]> {
  const api = new URL('v1/Admin/Requests', getAppConfigField('VITE_API'))
  const response = await fetch(api)
  if (!response.ok) return []
  return response.json() as Promise<AccessRequest[]>
}

export async function approveRequest (requestId: string): Promise<{ ok: boolean; redeemUrl: string | null }> {
  const api = new URL(`v1/Admin/Approve/${requestId}`, getAppConfigField('VITE_API'))
  const response = await fetch(api, { method: 'PUT' })
  if (!response.ok) return { ok: false, redeemUrl: null }
  const data = await response.json() as { redeemUrl?: string | null }
  return { ok: true, redeemUrl: data.redeemUrl ?? null }
}

export async function declineRequest (requestId: string): Promise<boolean> {
  const api = new URL(`v1/Admin/Decline/${requestId}`, getAppConfigField('VITE_API'))
  const response = await fetch(api, { method: 'PUT' })
  return response.ok
}
