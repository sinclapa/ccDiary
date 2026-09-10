import type { AppUser } from '@/services/models/appUser'
import { apiFetch, ApiUnavailableError, apiUrl, isUnavailable } from '@/services/modules/apiClient'

/**
 * Returns the caller's application user, or null when they do not have one.
 *
 * Throws rather than returning null when the API cannot be reached: a null here means the
 * user has no role and belongs in the registration flow, and a starting container must not
 * be allowed to look like that.
 */
export async function getMe (): Promise<AppUser | null> {
  const response = await apiFetch(apiUrl('v1/User/Me'))
  if (isUnavailable(response)) throw new ApiUnavailableError(response.status)
  if (!response.ok) return null
  return response.json() as Promise<AppUser>
}
