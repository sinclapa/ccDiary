import { flushPromises, mount, shallowMount, VueWrapper } from '@vue/test-utils'
import { expect, test, vi, describe, beforeEach, afterEach, it, beforeAll, afterAll } from 'vitest'
import { createVuetify } from 'vuetify'
import { nextTick } from 'vue'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { state } from '../../../src/services/authentication/msalConfig'
import Component from '../../../src/pages/diaries/index.vue'
import { diaryAPI } from '../../../src/services/modules/diaryService'
import Diary from '../../../src/services/models/diary'

const vuetify = createVuetify({
  components,
  directives,
})

global.ResizeObserver = require('resize-observer-polyfill')

function createFetchResponse(data) {
  return { json: () => new Promise((resolve) => resolve(data)) }
}

//TODO: Mock service not fetch

describe('pages/diaries/index.vue Implementation Test', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    state.isAuthenticated = false
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

  it('Initialize with correct elements', () => {
    expect(wrapper.findComponent('header').html()).toContain(`Diaries`)
    expect(wrapper.text()).not.toContain(`Add Diary`)
    expect(document.getElementsByClassName('v-card-title').length).toEqual(0)
  })

  it('Display Add Diary button when authenticated', async () => {
    state.isAuthenticated = true
    await flushPromises()
    expect(wrapper.text()).toContain(`Add Diary`)
  })
})

describe('pages/diaries/index.vue with successful HTTP Get', () => {
  let wrapper: VueWrapper
  const realFetch = global.fetch
  beforeAll(() => {
    const diaryGetResponse = [
      {
        "diaryId": "0af38239-b24f-4fa9-f679-08dcc87078fb",
        "title": "Test Diary",
        "author": "A J Smith",
        "description": "First Test Diary"
      },
      {
        "diaryId": "f80a9774-ab8c-44fd-f67d-08dcc87078fb",
        "title": "80 Days Around the World",
        "author": "Jules Verne",
        "description": "Circumnavigation around the earth"
      },
      {
        "diaryId": "ca89c5cf-7699-4d1c-f67b-08dcc87078fb",
        "title": "To the Moon and Back",
        "author": "Tom Hanks",
        "description": "Filming Apollo 13"
      }
    ]

    global.fetch = vi.fn().mockResolvedValue(createFetchResponse(diaryGetResponse))
  })

  afterAll(() => {
    global.fetch = realFetch
  })
//TODO: Should look at https://vuetifyjs.com/en/components/dialogs/#props-attach to remove template
  beforeEach(() => {
    state.isAuthenticated = false
    vi.stubEnv('VITE_API', 'http://test')
    wrapper = mount({template: "<v-defaults-provider :defaults=\"{'VDialog':{'contained':true }}\"><tested-component/></v-defaults-provider>"}, {
      global: {
        components: {'tested-component': Component},
        plugins: [vuetify],
      },
    })
  })

  afterEach(() => {
    state.isAuthenticated = false
    wrapper.unmount()
  })

  it('Validate result table', async () => {
    await flushPromises()
    expect(wrapper.findAll('table>tbody>tr').length).toEqual(3)
    expect(wrapper.find('table>tbody').text()).toMatch('80 Days Around the World')
    expect(wrapper.find('table>tbody').text()).toMatch('Tom Hanks')
    expect(wrapper.find('table>tbody').text()).toMatch('Filming Apollo 13')
    expect(wrapper.findAll('table>tbody>button').length).toEqual(0)
  })

  it('Validate result table edit mode', async () => {
    state.isAuthenticated = true
    await flushPromises()
    expect(wrapper.findAll('table>tbody>tr>td>button').length).toBeGreaterThan(0)
  })

  it('Delete diary dialog', async () => {
    state.isAuthenticated = true
    await flushPromises()
    const deleteButton = wrapper.findComponent('#f80a9774-ab8c-44fd-f67d-08dcc87078fb_delete')
    expect(deleteButton.html()).toMatch("delete")
    deleteButton.trigger('click')
    await nextTick()
    expect(wrapper.find('.v-card-title').html()).toMatch("Are you sure you want to delete this diary?")
  })

  it('Edit diary dialog', async () => {
    state.isAuthenticated = true
    await flushPromises()
    const editButton = wrapper.findComponent('#f80a9774-ab8c-44fd-f67d-08dcc87078fb_edit')
    editButton.trigger('click')
    await nextTick()
    expect(wrapper.find('form').html()).toMatch("Edit Diary")
    expect(wrapper.find('form').html()).toMatch("80 Days Around the World")
    expect(wrapper.find('form').html()).toMatch("Jules Verne")
    expect(wrapper.find('form').html()).toMatch("Circumnavigation around the earth")
  })

  it('Add diary dialog', async () => {
    state.isAuthenticated = true
    await flushPromises()
    const addButton = wrapper.findComponent('header>*>button')
    addButton.trigger('click')
    await nextTick()
    expect(wrapper.find('form').html()).toMatch("Add Diary")
  })
})
