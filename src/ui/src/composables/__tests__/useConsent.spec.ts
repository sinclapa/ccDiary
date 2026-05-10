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
})
