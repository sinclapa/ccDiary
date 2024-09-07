import { mount } from '@vue/test-utils'
import { expect, it, test, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppHeader from '../../src/components/AppHeader.vue'

const vuetify = createVuetify({
  components,
  directives,
})

global.ResizeObserver = require('resize-observer-polyfill')

test('Display AppHeader', async() => {

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
  const [drawer] = wrapper.findAll('nav')
  //expect(drawer.isVisible()).equals(false)
  // Assert the rendered text of the component
  expect(wrapper.findComponent('.v-app-bar-title').text()).toContain(`Cooking Code Diary`)
})
