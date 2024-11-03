import { mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { state } from '@/services/authentication/msalConfig'
import Component from '@/App.vue'

const vuetify = createVuetify({
  components,
  directives,
})

describe('App.vue Implementation Test', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    wrapper = mount(Component, {
      propsData: {},
      global: {
        plugins: [vuetify],
      },
    })
  })

  afterEach(() => {
    state.isAuthenticated = false
    wrapper.unmount()
  })

  it('Initialize with correct elements', () => {
    expect(wrapper.findComponent('main').exists()).toBeTruthy()
    expect(wrapper.find('router-view').exists()).toBeTruthy()
  })
})
