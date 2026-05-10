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

vi.mock('@/composables/useConsent', () => ({
  useConsent: () => ({ openPreferences: vi.fn(), bannerVisible: { value: false } }),
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
  expect(wrapper.text()).toContain(`© ${new Date().getFullYear()} Cooking Code`)
})

test('Displays Cookie preferences button in footer', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

  const wrapper = mount({
    template: '<v-layout><app-footer></app-footer></v-layout>',
  }, {
    props: {},
    global: {
      components: { AppFooter },
      plugins: [vuetify],
    },
  })

  expect(wrapper.text()).toContain('Cookie preferences')
})

test('Uses dark logo when theme is dark', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

  const darkVuetify = createVuetify({ components, directives, theme: { defaultTheme: 'dark' } })

  const wrapper = mount({
    template: '<v-layout><app-footer></app-footer></v-layout>',
  }, {
    global: {
      components: { AppFooter },
      plugins: [darkVuetify],
    },
  })

  const img = wrapper.find('img[alt="CookingCode"]')
  expect(img.attributes('src')).toContain('logo-simple-dark')
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
  expect(wrapper.text()).not.toContain(`test Version`)
})
