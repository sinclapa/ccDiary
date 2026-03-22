import { mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Component from '@/pages/index.vue'

const vuetify = createVuetify({
  components,
  directives,
})

describe('pages/Index.vue', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    wrapper = mount(Component, {
      global: {
        plugins: [vuetify],
      },
    })
  })

  afterEach(() => {
    wrapper.unmount()
  })

  it('renders the heading', () => {
    expect(wrapper.html()).toContain('Cooking Code Diary App')
  })

  it('renders the welcome text', () => {
    expect(wrapper.text()).toContain('Welcome to the Cooking Code Diary App')
  })

  it('renders the logo image', () => {
    expect(wrapper.findComponent({ name: 'VImg' }).exists()).toBe(true)
  })
})
