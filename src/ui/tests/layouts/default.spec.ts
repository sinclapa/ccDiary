import { describe, expect, it, vi } from 'vitest'
import { mount } from '@vue/test-utils'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { createPinia } from 'pinia'
import Component from '@/layouts/default.vue'

vi.mock('@/utils/appConfig', () => ({
  getAppConfigField: () => 'https://api.example.com/',
}))

const vuetify = createVuetify({
  components,
  directives,
})

describe('Default Layout', () => {
  it('renders header, main content, and footer', async () => {
    const wrapper = mount(Component, {
      shallow: false,
      propsData: {},
      global: {
        plugins: [vuetify, createPinia()],
        stubs: { ApiStatusBanner: true },
      },
    })

    expect(wrapper.html()).toContain('header')
    expect(wrapper.html()).toContain('main')
    expect(wrapper.html()).toContain('footer')
  })
})
