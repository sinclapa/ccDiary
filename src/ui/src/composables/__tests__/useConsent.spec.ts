import { beforeEach, describe, expect, it } from 'vitest'
import { FARO_CONSENT_KEY } from '@/plugins/faro'

describe('useConsent', () => {
  beforeEach(() => {
    localStorage.removeItem(FARO_CONSENT_KEY)
  })

  it('openPreferences removes consent key and sets bannerVisible to true', async () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')

    const { useConsent } = await import('../useConsent')
    const { bannerVisible, openPreferences } = useConsent()

    openPreferences()

    expect(localStorage.getItem(FARO_CONSENT_KEY)).toBeNull()
    expect(bannerVisible.value).toBe(true)
  })

  it('openPreferences sets currentStatus to "Accepted" when previous consent was true', async () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')

    const { useConsent } = await import('../useConsent')
    const { currentStatus, openPreferences } = useConsent()

    openPreferences()

    expect(currentStatus.value).toBe('Accepted')
  })

  it('openPreferences sets currentStatus to "Declined" when previous consent was false', async () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'false')

    const { useConsent } = await import('../useConsent')
    const { currentStatus, openPreferences } = useConsent()

    openPreferences()

    expect(currentStatus.value).toBe('Declined')
  })

  it('openPreferences sets currentStatus to null when no previous consent exists', async () => {
    const { useConsent } = await import('../useConsent')
    const { currentStatus, openPreferences } = useConsent()

    openPreferences()

    expect(currentStatus.value).toBeNull()
  })
})
