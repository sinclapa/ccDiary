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
    mapLocation: 'London, UK',
    showMap: false,
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

  it('emits submit with correct payload including mapLocation and showMap when form is valid', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, mapLocation: 'Paris, France', showMap: true },
      global: { plugins: [vuetify] },
    })

    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { location: string; entry: string; date: Date; mapLocation: string; showMap: boolean }
    expect(payload.location).toBe('Kitchen')
    expect(payload.entry).toBe('Cooked pasta')
    expect(payload.date.getHours()).toBe(12)
    expect(payload.date.getMinutes()).toBe(30)
    expect(payload.mapLocation).toBe('Paris, France')
    expect(payload.showMap).toBe(true)
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

  it('does not show map-location field when showMap is false', () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showMap: false },
      global: { plugins: [vuetify] },
    })
    expect(wrapper.find('#map-location').exists()).toBe(false)
  })

  it('shows map-location field when showMap is true', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showMap: true },
      global: { plugins: [vuetify] },
    })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#map-location').exists()).toBe(true)
  })

  it('updates mapLocation when prop changes', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showMap: true, mapLocation: 'Berlin, Germany' },
      global: { plugins: [vuetify] },
    })
    await wrapper.setProps({ mapLocation: 'Tokyo, Japan' })
    await wrapper.vm.$nextTick()
    expect((wrapper.find('#map-location').element as HTMLInputElement).value).toBe('Tokyo, Japan')
  })

  it('updates showMap when prop changes', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showMap: false },
      global: { plugins: [vuetify] },
    })
    expect(wrapper.find('#map-location').exists()).toBe(false)
    await wrapper.setProps({ showMap: true })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#map-location').exists()).toBe(true)
  })

  it('emits submit with showMap false and empty mapLocation by default', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, mapLocation: '', showMap: false },
      global: { plugins: [vuetify] },
    })
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { mapLocation: string; showMap: boolean }
    expect(payload.mapLocation).toBe('')
    expect(payload.showMap).toBe(false)
  })

  it('defaults mapLocation to location when showMap is toggled on with empty mapLocation', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, location: 'Kitchen', mapLocation: '', showMap: false },
      global: { plugins: [vuetify] },
    })
    // Toggle showMap on (simulates user clicking the switch)
    ;(wrapper.vm as any).showMap = true
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).mapLocation).toBe('Kitchen')
  })

  it('does not overwrite mapLocation when showMap is toggled on with an existing mapLocation', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, location: 'Kitchen', mapLocation: 'Paris, France', showMap: false },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showMap = true
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).mapLocation).toBe('Paris, France')
  })
})
