import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import DiaryEntryEditor from '@/components/DiaryEntryEditor.vue'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

const vuetify = createVuetify({ components, directives })

describe('DiaryEntryEditor.vue', () => {
  const defaultProps = {
    date: new Date(2024, 0, 1, 12, 30),
    location: 'Kitchen',
    entry: 'Cooked pasta',
  }

  it('renders form fields with initial props', () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: defaultProps,
      global: { plugins: [vuetify] },
    })
    expect((wrapper.find('#location').element as HTMLInputElement).value).toBe('Kitchen')
    expect((wrapper.find('#entry').element as HTMLInputElement).value).toBe('Cooked pasta')
    // Time field: Vuetify renders as input[type="time"]
    expect((wrapper.find('input[type="time"]').element as HTMLInputElement).value).toBe('12:30')
  })

  it('updates location and entry fields when props change', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    await wrapper.setProps({ location: 'Living Room', entry: 'Read a book' })
    await wrapper.vm.$nextTick()
    expect((wrapper.find('#location').element as HTMLInputElement).value).toBe('Living Room')
    expect((wrapper.find('#entry').element as HTMLInputElement).value).toBe('Read a book')
  })

  it('emits close when Close button is clicked', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: defaultProps,
      global: { plugins: [vuetify] },
    })
    await wrapper.find('#close').trigger('click')
    expect(wrapper.emitted('close')).toBeTruthy()
  })

  it('emits submit with correct payload when Save is clicked and form is valid', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: defaultProps,
      global: { plugins: [vuetify] },
    })

    // Simulate valid form submission
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { location: string; entry: string; date: Date }
    expect(payload.location).toBe('Kitchen')
    expect(payload.entry).toBe('Cooked pasta')
    expect(payload.date.getHours()).toBe(12)
    expect(payload.date.getMinutes()).toBe(30)
  })

  it('does not emit submit if form is invalid', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: defaultProps,
      global: { plugins: [vuetify] },
    })
    const submitEventPromise = Promise.resolve({ valid: false })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    expect(wrapper.emitted('submit')).toBeFalsy()
  })
})
