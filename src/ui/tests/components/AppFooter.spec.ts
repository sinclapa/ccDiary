import { mount } from '@vue/test-utils'
import { expect, test } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AppFooter from '@/components/AppFooter.vue'

const vuetify = createVuetify({
  components,
  directives,
})

globalThis.ResizeObserver = require('resize-observer-polyfill')

test('Display AppFooter', () => {
  const originalGlobalVersion = (globalThis as any).__APP_VERSION__
  ;(globalThis as any).__APP_VERSION__ = '1.2.3.789'

  try {
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

    expect(wrapper.text()).toContain(`Version 1.2.3.789 © 2023-${new Date().getFullYear()} CookingCode.com`)
  } finally {
    if (originalGlobalVersion === undefined) {
      delete (globalThis as any).__APP_VERSION__
    } else {
      ;(globalThis as any).__APP_VERSION__ = originalGlobalVersion
    }
  }
})
