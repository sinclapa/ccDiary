import { getAppConfigField } from '@/utils/appConfig'

export async function submitAccessRequest (displayName: string, email: string): Promise<void> {
  const api = new URL('v1/AccessRequest/Submit', getAppConfigField('VITE_API'))
  const response = await fetch(api, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ displayName, email }),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    throw new Error(body?.message ?? 'Failed to submit access request')
  }
}
