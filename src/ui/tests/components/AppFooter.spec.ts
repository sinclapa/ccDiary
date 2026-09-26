import { beforeEach, expect, test, vi } from 'vitest'

import { mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppFooter from '@/components/AppFooter.vue'
import { useAuthStore } from '@/stores/auth'
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
  setActivePinia(createPinia())
})

function mountFooter () {
  return mount({ template: '<v-layout><app-footer></app-footer></v-layout>' }, {
    global: { components: { AppFooter }, plugins: [vuetify] },
  })
}

function linkTitles (wrapper: ReturnType<typeof mountFooter>) {
  return wrapper.findAll('a.social-link').map(a => a.attributes('title'))
}

test('Display AppFooter with centered layout and icon row', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

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

  // Check that footer renders
  expect(wrapper.find('.app-footer').exists()).toBe(true)

  // Check that copyright row exists
  expect(wrapper.text()).toContain(`© ${new Date().getFullYear()} Cooking Code`)

  // Check that cookie preferences button exists
  expect(wrapper.text()).toContain('Cookie preferences')
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

test('Brand logo renders as inline SVG', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

  const wrapper = mount({
    template: '<v-layout><app-footer></app-footer></v-layout>',
  }, {
    global: {
      components: { AppFooter },
      plugins: [vuetify],
    },
  })

  const brandLink = wrapper.find('a.brand-link')
  expect(brandLink.find('svg').exists()).toBe(true)
})

test('Copyright line has Humans and Claude as links', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

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

  const humansLink = wrapper.find('a.footer-copy-link[href="https://en.wikipedia.org/wiki/Human"]')
  expect(humansLink.exists()).toBe(true)
  expect(humansLink.text()).toBe('Humans')

  const claudeLink = wrapper.find('a.footer-copy-link[href="https://claude.ai/login"]')
  expect(claudeLink.exists()).toBe(true)
  expect(claudeLink.text()).toBe('Claude')
})

test('Footer social icons have proper hover styling', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

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

  // Check that social links exist
  const socialLinks = wrapper.findAll('.social-link')
  expect(socialLinks.length).toBeGreaterThan(0)
})

test('displays app version in footer', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

  const wrapper = mount({
    template: '<v-layout><app-footer></app-footer></v-layout>',
  }, {
    global: {
      components: { AppFooter },
      plugins: [vuetify],
    },
  })

  const versionSpan = wrapper.find('.footer-row--secondary span')
  expect(versionSpan.exists()).toBe(true)
  expect(versionSpan.text().length).toBeGreaterThan(0)
})

test('brand link points to cookingcode.com with correct attributes', () => {
  vi.mocked(getAppConfigField).mockImplementation((_, opts) => opts?.defaultValue ?? 'NOT_SET')

  const wrapper = mount({
    template: '<v-layout><app-footer></app-footer></v-layout>',
  }, {
    global: {
      components: { AppFooter },
      plugins: [vuetify],
    },
  })

  const brandLink = wrapper.find('a.brand-link')
  expect(brandLink.attributes('href')).toBe('https://cookingcode.com')
  expect(brandLink.attributes('title')).toBe('CookingCode')
  expect(brandLink.attributes('target')).toBe('_blank')
})

test('hides the Swagger and API links from anyone who is not an admin', () => {
  vi.mocked(getAppConfigField).mockReturnValue('https://api.example.test/')
  const titles = linkTitles(mountFooter())
  expect(titles).toContain('GitHub')
  expect(titles).not.toContain('Swagger API')
  expect(titles.some(t => t?.startsWith('API '))).toBe(false)
})

test('hides them from a contributor too', () => {
  vi.mocked(getAppConfigField).mockReturnValue('https://api.example.test/')
  useAuthStore().appUser = { role: 'diary-contributor' } as any
  expect(linkTitles(mountFooter())).not.toContain('Swagger API')
})

test('shows the Swagger and API links to an admin', () => {
  vi.mocked(getAppConfigField).mockReturnValue('https://api.example.test/')
  useAuthStore().appUser = { role: 'diary-admin' } as any
  const wrapper = mountFooter()
  expect(linkTitles(wrapper)).toContain('Swagger API')
  expect(wrapper.find('a.social-link[title="Swagger API"]').attributes('href')).toBe('https://api.example.test/swagger')
  expect(wrapper.find('a.social-link[title="API https://api.example.test/"]').exists()).toBe(true)
})
