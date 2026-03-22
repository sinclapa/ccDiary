import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest'
import { mount } from '@vue/test-utils'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { setActivePinia, createPinia } from 'pinia'
import ApiStatusBanner from '@/components/ApiStatusBanner.vue'
import { useApiStatusStore } from '@/stores/apiStatus'

vi.mock('@/utils/appConfig', () => ({
  getAppConfigField: () => 'https://api.example.com/',
}))

const vuetify = createVuetify({ components, directives })

global.ResizeObserver = require('resize-observer-polyfill')

describe('ApiStatusBanner', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }))
  })

  afterEach(() => {
    const store = useApiStatusStore()
    store.stopPolling()
    vi.restoreAllMocks()
  })

  it('is hidden when API is available', async () => {
    const wrapper = mount({
      template: '<v-layout><api-status-banner /></v-layout>',
    }, {
      global: {
        components: { ApiStatusBanner },
        plugins: [vuetify],
      },
    })

    await wrapper.vm.$nextTick()

    expect(wrapper.text()).not.toContain('ingredients')
  })

  it('shows banner when API is unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const wrapper = mount({
      template: '<v-layout><api-status-banner /></v-layout>',
    }, {
      global: {
        components: { ApiStatusBanner },
        plugins: [vuetify],
      },
    })

    // Wait for onMounted checkHealth to complete
    await new Promise(resolve => setTimeout(resolve, 0))
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('ingredients')
  })

  it('calls checkHealth on mount', async () => {
    const store = useApiStatusStore()
    const spy = vi.spyOn(store, 'checkHealth')

    mount({
      template: '<v-layout><api-status-banner /></v-layout>',
    }, {
      global: {
        components: { ApiStatusBanner },
        plugins: [vuetify],
      },
    })

    expect(spy).toHaveBeenCalled()
  })

  it('registers fetch interceptor on mount', async () => {
    const store = useApiStatusStore()
    const spy = vi.spyOn(store, 'registerFetchInterceptor')

    mount({
      template: '<v-layout><api-status-banner /></v-layout>',
    }, {
      global: {
        components: { ApiStatusBanner },
        plugins: [vuetify],
      },
    })

    expect(spy).toHaveBeenCalled()
  })
})
