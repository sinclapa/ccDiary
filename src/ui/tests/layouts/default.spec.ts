import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Component from '@/layouts/default.vue'

const vuetify = createVuetify({
  components,
  directives,
})

describe('Default Layout', () => {
  it('renders header, main content, and footer', async () => {
    const wrapper = mount(Component, {
      shallow: false,
      propsData: {},
      global: {
        plugins: [vuetify],
      },
    })

    expect(wrapper.html()).toContain('header')
    expect(wrapper.html()).toContain('main')
    expect(wrapper.html()).toContain('footer')
  })
})
