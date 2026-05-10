import { beforeEach, describe, expect, test, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import ConsentBanner from '@/components/ConsentBanner.vue'
import { FARO_CONSENT_KEY } from '@/plugins/faro'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'

const { mockInitFaro } = vi.hoisted(() => ({ mockInitFaro: vi.fn() }))

vi.mock('@/plugins/faro', () => ({
  FARO_CONSENT_KEY: 'faro-consent',
  initFaro: mockInitFaro,
}))

vi.mock('@/composables/useConsent', () => {
  const bannerVisible = { value: true }
  return {
    useConsent: () => ({ bannerVisible }),
  }
})

globalThis.ResizeObserver = require('resize-observer-polyfill')

function mountBanner () {
  return mount({
    template: '<v-app><consent-banner /></v-app>',
  }, {
    attachTo: document.body,
    global: {
      components: { ConsentBanner },
      plugins: [vuetify],
    },
  })
}

describe('ConsentBanner', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.removeItem(FARO_CONSENT_KEY)
  })

  test('accept stores consent, initialises Faro, and hides banner', async () => {
    const wrapper = mountBanner()
    const buttons = document.querySelectorAll('.v-snackbar .v-btn')
    const acceptBtn = Array.from(buttons).find(b => b.textContent?.includes('Accept')) as HTMLElement
    acceptBtn.click()
    wrapper.unmount()

    expect(localStorage.getItem(FARO_CONSENT_KEY)).toBe('true')
    expect(mockInitFaro).toHaveBeenCalledOnce()
  })

  test('decline stores refusal and hides banner without initialising Faro', async () => {
    const wrapper = mountBanner()
    const buttons = document.querySelectorAll('.v-snackbar .v-btn')
    const declineBtn = Array.from(buttons).find(b => b.textContent?.includes('Decline')) as HTMLElement
    declineBtn.click()
    wrapper.unmount()

    expect(localStorage.getItem(FARO_CONSENT_KEY)).toBe('false')
    expect(mockInitFaro).not.toHaveBeenCalled()
  })
})
