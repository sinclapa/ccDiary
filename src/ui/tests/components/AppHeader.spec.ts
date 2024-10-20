import { mount } from '@vue/test-utils'
import { afterEach, expect, test, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppHeader from '../../src/components/AppHeader.vue'
import { state } from '../../src/services/authentication/msalConfig'
import { AccountInfo } from '@azure/msal-browser'
import { describe } from 'node:test'

const vuetify = createVuetify({
  components,
  directives,
})

vi.mock('../../src/services/authentication/msalConfig')

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
})
