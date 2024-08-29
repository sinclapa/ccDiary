import { mount } from '@vue/test-utils'
import { expect, test } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppFooter from '../../src/components/AppFooter.vue'

const vuetify = createVuetify({
  components,
  directives,
})

global.ResizeObserver = require('resize-observer-polyfill')

test('displays message', () => {
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
  expect(wrapper.text()).toContain('CookingCode.com')
})
