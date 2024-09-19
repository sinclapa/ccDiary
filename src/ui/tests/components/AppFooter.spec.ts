import { mount } from '@vue/test-utils'
import { expect, test, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppFooter from '../../src/components/AppFooter.vue'

const vuetify = createVuetify({
  components,
  directives,
})

global.ResizeObserver = require('resize-observer-polyfill')

test('Display AppFooter', () => {
  vi.stubEnv('VITE_VERSION', '1.2.3')
  vi.stubEnv('VITE_BUILDNUMBER', '789')
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

  // Assert the rendered text of the component
  expect(wrapper.text()).toContain(`Version 1.2.3.789 © 2023-${new Date().getFullYear()} CookingCode.com`)
})
