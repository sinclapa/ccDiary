import { flushPromises, mount } from '@vue/test-utils'
import { describe, expect, test } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import DiaryEditor from '@/components/DiaryEditor.vue'

const vuetify = createVuetify({
  components,
  directives,
})

globalThis.ResizeObserver = require('resize-observer-polyfill')
describe('DiaryEditor', () => {
  test('Pass Props', async () => {
    const wrapper = mount(DiaryEditor, {
      props: {
        title: 'Original Title',
        author: 'Original Author',
        description: 'Original Description',
        addMode: false,
      },
      global: {
        plugins: [vuetify],
      },
    })

    // Validate original values
    expect(wrapper.find('#title').attributes('value')).equals('Original Title')
    expect(wrapper.find('#author').attributes('value')).equals('Original Author')
    expect(wrapper.find('#description').attributes('value')).equals('Original Description')
  })

  test('ChangeValuesAndSubmit', async () => {
    const wrapper = mount(DiaryEditor, {
      props: {
        title: 'Original Title',
        author: 'Original Author',
        description: 'Original Description',
        addMode: false,
      },
      global: {
        plugins: [vuetify],
      },
    })

    const title = wrapper.find('#title')
    const author = wrapper.find('#author')
    const description = wrapper.find('#description')

    // Change Values
    title.setValue('New Title')
    author.setValue('New Author')
    description.setValue('New Description')

    // Submit Form
    wrapper.find('#save').trigger('submit')
    await flushPromises()
    expect(wrapper.emitted().submit).toHaveLength(1)
    expect(wrapper.emitted().submit[0]).toEqual([
      {
        title: 'New Title',
        author: 'New Author',
        description: 'New Description',
      },
    ])
  })

  test('CloseForm', async () => {
    const wrapper = mount(DiaryEditor, {
      props: {
        title: 'Original Title',
        author: 'Original Author',
        description: 'Original Description',
        addMode: false,
      },
      global: {
        plugins: [vuetify],
      },
    })

    // Close Form
    wrapper.find('#close').trigger('click')
    expect(wrapper.emitted().close).toHaveLength(1)
  })

  test('ValidationFailureTitleTooShort', async () => {
    const wrapper = mount(DiaryEditor, {
      props: {
        title: '',
        author: '',
        description: '',
        addMode: false,
      },
      global: {
        plugins: [vuetify],
      },
    })
    const title = wrapper.find('#title')

    expect(wrapper.find('#title-messages').text()).equals('')

    // Change Bad values
    title.setValue('1234')
    wrapper.find('#save').trigger('submit')
    await flushPromises()
    expect(wrapper.find('#title-messages').text()).toContain('Title must be at least 5 characters')
  })

  test('ValidationFailureAuthorTooShort', async () => {
    const wrapper = mount(DiaryEditor, {
      props: {
        title: '',
        author: '',
        description: '',
        addMode: false,
      },
      global: {
        plugins: [vuetify],
      },
    })
    const title = wrapper.find('#author')

    expect(wrapper.find('#author-messages').text()).equals('')

    // Change Bad values
    title.setValue('1234')
    wrapper.find('#save').trigger('submit')
    await flushPromises()
    expect(wrapper.find('#author-messages').text()).toContain('Author must be at least 5 characters')
  })
})
