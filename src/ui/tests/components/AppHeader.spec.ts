import { mount } from '@vue/test-utils'
import { afterEach, expect, test, vi } from 'vitest'
import AppHeader from '@/components/AppHeader.vue'
import { state } from '@/services/authentication/msalConfig'
import { AccountInfo } from '@azure/msal-browser'
import { describe } from 'node:test'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'

vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({
    isAdmin: false,
    isContributor: false,
    appUser: null,
    fetchAppUser: vi.fn().mockResolvedValue(undefined),
    clearAppUser: vi.fn(),
  }),
}))

vi.mock('@/services/authentication/msalConfig')
const msalServiceSpies = {
  initializeInstance: vi.fn().mockImplementation(() => { throw new Error('MSAL instance initialization failed') }),
  login: vi.fn(),
  logout: vi.fn(),
  handleRedirect: vi.fn().mockResolvedValue(null),
  registerAuthorizationHeaderInterceptor: vi.fn().mockResolvedValue(null),
}

vi.mock('@/services/authentication/msalService', () => {
  return {
    msalService: () => msalServiceSpies,
  }
})

globalThis.ResizeObserver = require('resize-observer-polyfill')
describe('AppHeader', () => {
  afterEach(() => {
    vi.resetAllMocks()
  })

  test('Logged Out', async () => {
    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      props: {},
      global: {
        components: {
          AppHeader,
        },
        plugins: [vuetify],
      },
    })
    const loginButton = wrapper.findAllComponents('#login')
    expect(loginButton.length).equals(1)
    loginButton[0].trigger('click')
  })

  test('Logged In', async () => {
    state.isAuthenticated = true
    state.user = { name: 'Jane Doe' } as AccountInfo

    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      props: {},
      global: {
        components: {
          AppHeader,
        },
        plugins: [vuetify],
      },
    })
    expect(wrapper.find('#username').text()).toContain(`Jane Doe`)
    expect(wrapper.findComponent('.v-app-bar-title').text()).toContain(`Cooking Code Diary`)
    const logoutButton = wrapper.findAllComponents('#logout')
    expect(logoutButton.length).equals(1)
    logoutButton[0].trigger('click')
  })

  test('ShowDrawer', async () => {
    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      props: {},
      global: {
        components: {
          AppHeader,
        },
        plugins: [vuetify],
      },
    })

    const drawerButton = wrapper.findComponent('.v-app-bar-nav-icon')
    drawerButton.trigger('click')
  })

  test('drawer location is "top" on mobile', async () => {
    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      global: {
        components: { AppHeader },
        plugins: [vuetify],
        mocks: { $vuetify: { display: { mobile: true } } },
      },
    })
    const drawer = wrapper.findComponent({ name: 'VNavigationDrawer' })
    expect(drawer.props('location')).toBe('top')
  })

  test('drawer location is not "top" on non-mobile', async () => {
    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      global: {
        components: { AppHeader },
        plugins: [vuetify],
        mocks: { $vuetify: { display: { mobile: false } } },
      },
    })
    const drawer = wrapper.findComponent({ name: 'VNavigationDrawer' })
    expect(drawer.props('location')).not.toBe('top')
  })

  test('theme toggle button switches theme and saves preference', async () => {
    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      global: {
        components: { AppHeader },
        plugins: [vuetify],
      },
    })

    const toggleBtn = wrapper.find('#theme-toggle')
    expect(toggleBtn.exists()).toBe(true)
    await toggleBtn.trigger('click')
  })

  test('calls initialize, handleRedirect, and registerAuthorizationHeaderInterceptor on mount', async () => {
    // Clear call history before test
    Object.values(msalServiceSpies).forEach(fn => fn.mockClear())
    mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      global: {
        components: { AppHeader },
        plugins: [vuetify],
      },
    })
    await new Promise(resolve => setTimeout(resolve, 0))
    expect(msalServiceSpies.initializeInstance).toHaveBeenCalled()
    expect(msalServiceSpies.handleRedirect).toHaveBeenCalled()
    expect(msalServiceSpies.registerAuthorizationHeaderInterceptor).toHaveBeenCalled()
  })
})
