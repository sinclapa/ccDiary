import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { createPinia, setActivePinia } from 'pinia'
import ApiStatusBanner from '@/components/ApiStatusBanner.vue'
import { useApiStatusStore } from '@/stores/apiStatus'

vi.mock('@/utils/appConfig', () => ({
  getAppConfigField: () => 'https://api.example.com/',
}))

const vuetify = createVuetify({ components, directives })

globalThis.ResizeObserver = require('resize-observer-polyfill')

describe('ApiStatusBanner', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.useFakeTimers()
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: true }))
  })

  afterEach(() => {
    const store = useApiStatusStore()
    store.stopPolling()
    vi.useRealTimers()
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

    await flushPromises()
    await wrapper.vm.$nextTick()

    expect(wrapper.text()).toContain('ingredients')
  })

  it('shows elapsed seconds counter when API is unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const wrapper = mount({
      template: '<v-layout><api-status-banner /></v-layout>',
    }, {
      global: {
        components: { ApiStatusBanner },
        plugins: [vuetify],
      },
    })

    await flushPromises()
    await wrapper.vm.$nextTick()
    expect(wrapper.text()).toContain('(0s)')

    await vi.advanceTimersByTimeAsync(3000)
    await wrapper.vm.$nextTick()
    expect(wrapper.text()).toContain('(3s)')
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

  it('stops counter when API becomes available again after being unavailable', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const wrapper = mount({
      template: '<v-layout><api-status-banner /></v-layout>',
    }, {
      global: {
        components: { ApiStatusBanner },
        plugins: [vuetify],
      },
    })

    await flushPromises()
    await wrapper.vm.$nextTick()

    // Banner is visible, counter is running
    expect(wrapper.text()).toContain('ingredients')

    // Make the API available again — this triggers the watch to call stopCounter
    const store = useApiStatusStore()
    store.setAvailable(true)
    await wrapper.vm.$nextTick()

    // Banner should now be hidden
    expect(wrapper.text()).not.toContain('ingredients')
  })

  it('calls stopCounter on unmount when counter is running', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))

    const wrapper = mount({
      template: '<v-layout><api-status-banner /></v-layout>',
    }, {
      global: {
        components: { ApiStatusBanner },
        plugins: [vuetify],
      },
    })

    await flushPromises()
    await wrapper.vm.$nextTick()

    // Counter should be running (banner visible)
    expect(wrapper.text()).toContain('ingredients')

    const clearIntervalSpy = vi.spyOn(globalThis, 'clearInterval')

    // Unmount should trigger onUnmounted -> stopCounter -> clearInterval
    wrapper.unmount()

    expect(clearIntervalSpy).toHaveBeenCalled()
  })
})
