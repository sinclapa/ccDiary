import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { state } from '../../src/services/authentication/msalConfig'
import Component from '../../src/pages/index.vue'

const vuetify = createVuetify({
  components,
  directives,
})

function createFetchResponse (data: any) {
  return { json: () => new Promise(resolve => resolve(data)) }
}

describe('pages/Index.vue Implementation Test', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    state.isAuthenticated = false
    vi.stubEnv('VITE_API', 'http://test')
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
    expect(wrapper.html()).toContain(`Weather App`)
    expect(wrapper.findComponent('button').exists()).toBeFalsy()
  })

  it('Display button when authenticated', async () => {
    state.isAuthenticated = true
    await flushPromises()
    expect(wrapper.findComponent('button').exists()).toBeTruthy()
  })
})

describe('Pages/Index.vue with successful HTTP Get', () => {
  let wrapper: VueWrapper
  const realFetch = global.fetch
  beforeAll(() => {
    const weatherResponse = [
      {
        date: '2024-10-21',
        temperatureC: 29,
        temperatureF: 84,
        summary: 'Sweltering',
      },
      {
        date: '2024-10-22',
        temperatureC: 53,
        temperatureF: 127,
        summary: 'Balmy',
      },
      {
        date: '2024-10-23',
        temperatureC: 44,
        temperatureF: 111,
        summary: 'Balmy',
      },
      {
        date: '2024-10-24',
        temperatureC: -6,
        temperatureF: 22,
        summary: 'Freezing',
      },
      {
        date: '2024-10-25',
        temperatureC: 30,
        temperatureF: 85,
        summary: 'Freezing',
      },
    ]

    global.fetch = vi.fn().mockResolvedValue(createFetchResponse(weatherResponse))
  })

  afterAll(() => {
    global.fetch = realFetch
  })

  beforeEach(() => {
    state.isAuthenticated = true
    vi.stubEnv('VITE_API', 'http://test')
    wrapper = mount(Component, {
      global: {
        plugins: [vuetify],
      },
    })
  })

  afterEach(() => {
    state.isAuthenticated = false
    wrapper.unmount()
  })

  it('Validate result table', async () => {
    wrapper.findComponent('button').trigger('click')
    await flushPromises()
    expect(wrapper.findAll('tbody>tr').length).toEqual(5)
    expect(wrapper.findAll('tbody>tr>td')[3].text()).toMatch('Sweltering')
    expect(wrapper.findAll('tbody>tr>td')[4].text()).toMatch('2024-10-22')
    expect(wrapper.findAll('tbody>tr>td')[9].text()).toMatch('44')
    expect(wrapper.findAll('tbody>tr>td')[14].text()).toMatch('22')
  })
})
