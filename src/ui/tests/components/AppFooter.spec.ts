import { beforeEach, expect, test, vi } from 'vitest'

import { mount } from '@vue/test-utils'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppFooter from '@/components/AppFooter.vue'
import { getAppConfigField } from '@/utils/appConfig'

vi.mock('@/utils/appConfig', () => ({
  getAppConfigField: vi.fn(),
}))

const vuetify = createVuetify({
  components,
  directives,
})

globalThis.ResizeObserver = require('resize-observer-polyfill')

beforeEach(() => {
  vi.clearAllMocks()
})

test('Display AppFooter with environment and version', () => {
  vi.mocked(getAppConfigField).mockImplementation((field, opts) => {
    if (field === 'VITE_ENVIRONMENT') return 'test'
    return opts?.defaultValue ?? 'NOT_SET'
  })

  const wrapper = mount({
    template: '<v-layout><app-footer></app-footer></v-layout>',
  }, {
    props: {},
    global: {
      components: {
        AppFooter,
      },
      plugins: [vuetify],
    },
  })

  expect(wrapper.text()).toContain(`test Version ${__APP_VERSION__}`)
  expect(wrapper.text()).toContain(`© 2023-${new Date().getFullYear()} CookingCode.com`)
})

test('Display AppFooter without environment prefix when environment not set', () => {
  vi.mocked(getAppConfigField).mockImplementation((field, opts) => {
    if (field === 'VITE_ENVIRONMENT') return ''
    return opts?.defaultValue ?? 'NOT_SET'
  })

  const wrapper = mount({
    template: '<v-layout><app-footer></app-footer></v-layout>',
  }, {
    props: {},
    global: {
      components: {
        AppFooter,
      },
      plugins: [vuetify],
    },
  })

  expect(wrapper.text()).toContain(`Version ${__APP_VERSION__}`)
  expect(wrapper.text()).not.toMatch(/\S+ Version/)
})
