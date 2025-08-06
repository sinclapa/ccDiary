import { mount } from '@vue/test-utils'
import { afterEach, expect, test, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppHeader from '@/components/AppHeader.vue'
import { state } from '@/services/authentication/msalConfig'
import { AccountInfo } from '@azure/msal-browser'
import { describe } from 'node:test'

const vuetify = createVuetify({
  components,
  directives,
})

vi.mock('@/services/authentication/msalConfig')
const msalServiceSpies = {
  initializeInstance: vi.fn().mockImplementation(()=> { throw new Error('MSAL instance initialization failed') }),
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

global.ResizeObserver = require('resize-observer-polyfill')
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

  test('drawer location is "bottom" on mobile', async () => {
    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      global: {
        components: { AppHeader },
        plugins: [vuetify],
        mocks: {
          $vuetify: {
            display: { mobile: true }
          }
        }
      }
    })
    // Find the navigation drawer
    const drawer = wrapper.findComponent({ name: 'VNavigationDrawer' })
    // The prop should be "bottom"
    expect(drawer.props('location')).toBe('bottom')
  })

  test('drawer location is not "bottom" on non-mobile', async () => {
    const wrapper = mount({
      template: '<v-layout><app-header></app-header></v-layout>',
    }, {
      global: {
        components: { AppHeader },
        plugins: [vuetify],
        mocks: {
          $vuetify: {
            display: { mobile: false }
          }
        }
      }
    })
    // Find the navigation drawer
    const drawer = wrapper.findComponent({ name: 'VNavigationDrawer' })
    // The prop should be "bottom"
    expect(drawer.props('location')).not.toBe('bottom')
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
