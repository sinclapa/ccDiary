import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import AppHeader from '@/components/AppHeader.vue'
import { state } from '@/services/authentication/msalConfig'
import { AccountInfo } from '@azure/msal-browser'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'

const mockReplace = vi.fn()
const mockCurrentRoute = { value: { path: '/' } }

vi.mock('vue-router', () => ({
  useRouter: () => ({
    replace: mockReplace,
    currentRoute: mockCurrentRoute,
  }),
}))

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
    state.isAuthenticated = false
    state.user = null
    mockCurrentRoute.value = { path: '/' }
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
    const logoLink = wrapper.find('.logo-link')
    expect(logoLink.find('.logo-cc').text()).toBe('cc')
    expect(logoLink.find('.logo-diary').text()).toBe('Diary')
    const logoutButton = wrapper.findAllComponents('#logout')
    expect(logoutButton.length).equals(1)
    logoutButton[0].trigger('click')
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

  const mountHeader = () => mount(
    { template: '<v-layout><app-header></app-header></v-layout>' },
    { global: { components: { AppHeader }, plugins: [vuetify] } },
  )

  describe('handleLogin redirect', () => {
    beforeEach(() => {
      msalServiceSpies.handleRedirect.mockResolvedValue(null)
      msalServiceSpies.registerAuthorizationHeaderInterceptor.mockResolvedValue(undefined)
    })

    test('redirects to / after login when on /register', async () => {
      mockCurrentRoute.value = { path: '/register' }
      msalServiceSpies.login.mockImplementation(() => { state.isAuthenticated = true })

      const wrapper = mountHeader()
      await flushPromises()
      mockReplace.mockClear()

      await wrapper.find('#login').trigger('click')
      await flushPromises()

      expect(mockReplace).toHaveBeenCalledWith('/')
    })

    test('does not redirect after login when not on /register', async () => {
      mockCurrentRoute.value = { path: '/' }
      msalServiceSpies.login.mockImplementation(() => { state.isAuthenticated = true })

      const wrapper = mountHeader()
      await flushPromises()
      mockReplace.mockClear()

      await wrapper.find('#login').trigger('click')
      await flushPromises()

      expect(mockReplace).not.toHaveBeenCalled()
    })
  })

  describe('handleLogout redirect', () => {
    beforeEach(() => {
      state.isAuthenticated = true
      state.user = { name: 'Test User' } as AccountInfo
      msalServiceSpies.handleRedirect.mockResolvedValue(null)
      msalServiceSpies.registerAuthorizationHeaderInterceptor.mockResolvedValue(undefined)
    })

    test('redirects to / after logout when on /admin', async () => {
      mockCurrentRoute.value = { path: '/admin' }

      const wrapper = mountHeader()
      await flushPromises()
      mockReplace.mockClear()

      await wrapper.find('#logout').trigger('click')
      await flushPromises()

      expect(mockReplace).toHaveBeenCalledWith('/')
    })

    test('also redirects when on a nested /admin route', async () => {
      mockCurrentRoute.value = { path: '/admin/users' }

      const wrapper = mountHeader()
      await flushPromises()
      mockReplace.mockClear()

      await wrapper.find('#logout').trigger('click')
      await flushPromises()

      expect(mockReplace).toHaveBeenCalledWith('/')
    })

    test('does not redirect after logout when not on /admin', async () => {
      mockCurrentRoute.value = { path: '/diaries' }

      const wrapper = mountHeader()
      await flushPromises()
      mockReplace.mockClear()

      await wrapper.find('#logout').trigger('click')
      await flushPromises()

      expect(mockReplace).not.toHaveBeenCalled()
    })
  })

  describe('handleRedirect navigation', () => {
    beforeEach(() => {
      msalServiceSpies.registerAuthorizationHeaderInterceptor.mockResolvedValue(undefined)
    })

    test('redirects to / when redirect-based login returns /register as state', async () => {
      msalServiceSpies.handleRedirect.mockResolvedValue('/register')

      mountHeader()
      await flushPromises()

      expect(mockReplace).toHaveBeenCalledWith('/')
    })

    test('navigates to the returned path when not /register', async () => {
      msalServiceSpies.handleRedirect.mockResolvedValue('/diaries/abc-123')

      mountHeader()
      await flushPromises()

      expect(mockReplace).toHaveBeenCalledWith('/diaries/abc-123')
    })

    test('does not navigate when redirect returns null', async () => {
      msalServiceSpies.handleRedirect.mockResolvedValue(null)

      mountHeader()
      await flushPromises()

      expect(mockReplace).not.toHaveBeenCalled()
    })
  })
})
