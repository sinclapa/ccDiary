import { apiFetch, apiUrl } from '@/services/modules/apiClient'

export async function submitAccessRequest (displayName: string, email: string): Promise<void> {
  const api = apiUrl('v1/AccessRequest/Submit')
  const response = await apiFetch(api, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ displayName, email }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new Error(body?.message ?? 'Failed to submit access request')
  }
}
