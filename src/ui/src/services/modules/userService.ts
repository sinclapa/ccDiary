import type { AppUser } from '@/services/models/appUser'
import { getAppConfigField } from '@/utils/appConfig'

export async function getMe (): Promise<AppUser | null> {
  const api = new URL('v1/User/Me', getAppConfigField('VITE_API'))
  const response = await fetch(api)
  if (!response.ok) return null
  return response.json() as Promise<AppUser>
}
