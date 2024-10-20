import { mount } from '@vue/test-utils'
import { expect, test, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { state } from '../../src/services/authentication/msalConfig'
import Index from '../../src/pages/index.vue'

const vuetify = createVuetify({
  components,
  directives,
})

global.ResizeObserver = require('resize-observer-polyfill')

test('Display PageIndex', () => {
  vi.stubEnv('VITE_API', 'http://test')
  const wrapper = mount(Index, {
    props: {},
    global: {
      plugins: [vuetify],
    },
  })

  // Assert the rendered text of the component
  expect(wrapper.html()).toContain(`http://test/v1/WeatherForecast/Get`)
  expect(wrapper.findComponent('.v-btn').exists()).toBeFalsy()
})

test('Display PageIndex Authenticated', () => {
  state.isAuthenticated = true
  vi.stubEnv('VITE_API', 'http://test')
  const wrapper = mount(Index, {
    props: {},
    global: {
      plugins: [vuetify],
    },
  })

  // Assert the rendered text of the component
  expect(wrapper.findComponent('.v-btn').exists()).toBeTruthy()
})

