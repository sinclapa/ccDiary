import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
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
    fromLocation: '',
    toLocation: '',
    showJourney: false,
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

  it('does not show from-location field when showJourney is false', () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: false },
      global: { plugins: [vuetify] },
    })
    expect(wrapper.find('#from-location').exists()).toBe(false)
    expect(wrapper.find('#to-location').exists()).toBe(false)
  })

  it('shows from-location and to-location fields when showJourney is true', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true, fromLocation: 'Sandwich, UK', toLocation: 'Southampton, UK' },
      global: { plugins: [vuetify] },
    })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#from-location').exists()).toBe(true)
    expect(wrapper.find('#to-location').exists()).toBe(true)
  })

  it('emits submit with showJourney, fromLocation, toLocation in payload', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true, fromLocation: 'Sandwich, UK', toLocation: 'Southampton, UK' },
      global: { plugins: [vuetify] },
    })
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { showJourney: boolean; fromLocation: string; toLocation: string }
    expect(payload.showJourney).toBe(true)
    expect(payload.fromLocation).toBe('Sandwich, UK')
    expect(payload.toLocation).toBe('Southampton, UK')
  })

  it('emits submit with showJourney false and empty locations by default', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: false, fromLocation: '', toLocation: '' },
      global: { plugins: [vuetify] },
    })
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { showJourney: boolean; fromLocation: string; toLocation: string }
    expect(payload.showJourney).toBe(false)
    expect(payload.fromLocation).toBe('')
    expect(payload.toLocation).toBe('')
  })

  it('defaults fromLocation to location when showJourney is toggled on with empty fromLocation', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, location: 'Kitchen', fromLocation: '', showJourney: false },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showJourney = true
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).fromLocation).toBe('Kitchen')
  })

  it('does not overwrite fromLocation when showJourney is toggled on with an existing fromLocation', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, location: 'Kitchen', fromLocation: 'Sandwich, UK', showJourney: false },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showJourney = true
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).fromLocation).toBe('Sandwich, UK')
  })

  it('updates fromLocation when prop changes', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true, fromLocation: 'Sandwich, UK', toLocation: 'Southampton, UK' },
      global: { plugins: [vuetify] },
    })
    await wrapper.setProps({ fromLocation: 'London, UK' })
    await wrapper.vm.$nextTick()
    expect((wrapper.find('#from-location').element as HTMLInputElement).value).toBe('London, UK')
  })

  it('updates toLocation when prop changes', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true, fromLocation: 'Sandwich, UK', toLocation: 'Southampton, UK' },
      global: { plugins: [vuetify] },
    })
    await wrapper.setProps({ toLocation: 'Paris, France' })
    await wrapper.vm.$nextTick()
    expect((wrapper.find('#to-location').element as HTMLInputElement).value).toBe('Paris, France')
  })

  it('updates showJourney when prop changes', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: false },
      global: { plugins: [vuetify] },
    })
    expect(wrapper.find('#from-location').exists()).toBe(false)
    await wrapper.setProps({ showJourney: true })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#from-location').exists()).toBe(true)
  })

  it('does not default fromLocation when showJourney is toggled off', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, location: 'Kitchen', fromLocation: '', showJourney: true },
      global: { plugins: [vuetify] },
    })
    // Toggle showJourney off — watcher fires with newVal=false, condition short-circuits
    ;(wrapper.vm as any).showJourney = false
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).fromLocation).toBe('')
  })

  it('does not default mapLocation when showMap is toggled off', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, location: 'Kitchen', mapLocation: '', showMap: true },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showMap = false
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).mapLocation).toBe('')
  })

  it('does not show image drop zone when showImage is false', () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    expect(wrapper.find('#image-drop-zone').exists()).toBe(false)
  })

  it('shows image drop zone when showImage is toggled on', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showImage = true
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#image-drop-zone').exists()).toBe(true)
  })

  it('shows image drop zone when imageData prop is provided', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, imageData: 'abc123', imageContentType: 'image/jpeg' },
      global: { plugins: [vuetify] },
    })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#image-drop-zone').exists()).toBe(true)
  })

  it('clears image data when showImage is toggled off', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, imageData: 'abc123', imageContentType: 'image/jpeg' },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showImage = false
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).imageData).toBeUndefined()
    expect((wrapper.vm as any).imageContentType).toBeUndefined()
  })

  it('emits submit with imageData and imageContentType in payload', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, imageData: 'abc123', imageContentType: 'image/jpeg' },
      global: { plugins: [vuetify] },
    })
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { imageData: string | undefined; imageContentType: string | undefined }
    expect(payload.imageData).toBe('abc123')
    expect(payload.imageContentType).toBe('image/jpeg')
  })

  it('emits submit with undefined imageData and imageContentType by default', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { imageData: string | undefined; imageContentType: string | undefined }
    expect(payload.imageData).toBeUndefined()
    expect(payload.imageContentType).toBeUndefined()
  })

  it('updates imageData when prop changes', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, imageData: 'abc123', imageContentType: 'image/jpeg' },
      global: { plugins: [vuetify] },
    })
    await wrapper.setProps({ imageData: 'xyz789' })
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).imageData).toBe('xyz789')
  })

  it('clearImage clears image state and hides image section', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, imageData: 'abc123', imageContentType: 'image/jpeg' },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).clearImage()
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).imageData).toBeUndefined()
    expect((wrapper.vm as any).imageContentType).toBeUndefined()
    expect((wrapper.vm as any).showImage).toBe(false)
  })

  it('triggerFileInput clicks the file input element', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showImage = true
    await wrapper.vm.$nextTick()
    const clickSpy = vi.fn()
    ;(wrapper.vm as any).fileInputRef = { click: clickSpy }
    ;(wrapper.vm as any).triggerFileInput()
    expect(clickSpy).toHaveBeenCalled()
  })

  it('Enter key on drop zone triggers file input', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).showImage = true
    await wrapper.vm.$nextTick()
    const clickSpy = vi.fn()
    ;(wrapper.vm as any).fileInputRef = { click: clickSpy }
    await wrapper.find('#image-drop-zone').trigger('keydown.enter')
    expect(clickSpy).toHaveBeenCalled()
  })

  it('processFile reads file and sets imageData and imageContentType', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    let capturedOnload: ((e: any) => void) | undefined
    const mockReader = { readAsDataURL: vi.fn(), get onload () { return capturedOnload! }, set onload (cb: (e: any) => void) { capturedOnload = cb } }
    vi.spyOn(globalThis, 'FileReader').mockImplementation(() => mockReader as any)

    const file = new File(['dummy'], 'test.jpg', { type: 'image/jpeg' })
    ;(wrapper.vm as any).processFile(file)
    capturedOnload?.({ target: { result: 'data:image/jpeg;base64,abc123' } })
    await wrapper.vm.$nextTick()

    expect((wrapper.vm as any).imageData).toBe('abc123')
    expect((wrapper.vm as any).imageContentType).toBe('image/jpeg')
  })

  it('handleDrop processes dropped image file', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    let capturedOnload: ((e: any) => void) | undefined
    const mockReader = { readAsDataURL: vi.fn(), get onload () { return capturedOnload! }, set onload (cb: (e: any) => void) { capturedOnload = cb } }
    vi.spyOn(globalThis, 'FileReader').mockImplementation(() => mockReader as any)

    const file = new File(['dummy'], 'test.png', { type: 'image/png' })
    ;(wrapper.vm as any).handleDrop({ dataTransfer: { files: [file] } })
    capturedOnload?.({ target: { result: 'data:image/png;base64,xyz789' } })
    await wrapper.vm.$nextTick()

    expect((wrapper.vm as any).imageData).toBe('xyz789')
    expect((wrapper.vm as any).imageContentType).toBe('image/png')
    expect((wrapper.vm as any).isDragging).toBe(false)
  })

  it('handleDrop ignores non-image files', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    const file = new File(['dummy'], 'test.txt', { type: 'text/plain' })
    ;(wrapper.vm as any).handleDrop({ dataTransfer: { files: [file] } })
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).imageData).toBeUndefined()
  })

  it('handleFileSelect processes selected image file', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    let capturedOnload: ((e: any) => void) | undefined
    const mockReader = { readAsDataURL: vi.fn(), get onload () { return capturedOnload! }, set onload (cb: (e: any) => void) { capturedOnload = cb } }
    vi.spyOn(globalThis, 'FileReader').mockImplementation(() => mockReader as any)

    const file = new File(['dummy'], 'selected.jpg', { type: 'image/jpeg' })
    ;(wrapper.vm as any).handleFileSelect({ target: { files: [file] } })
    capturedOnload?.({ target: { result: 'data:image/jpeg;base64,selected123' } })
    await wrapper.vm.$nextTick()

    expect((wrapper.vm as any).imageData).toBe('selected123')
    expect((wrapper.vm as any).imageContentType).toBe('image/jpeg')
  })

  it('handleWindowPaste processes pasted image and shows image section', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    let capturedOnload: ((e: any) => void) | undefined
    const mockReader = { readAsDataURL: vi.fn(), get onload () { return capturedOnload! }, set onload (cb: (e: any) => void) { capturedOnload = cb } }
    vi.spyOn(globalThis, 'FileReader').mockImplementation(() => mockReader as any)

    const file = new File(['dummy'], 'pasted.jpg', { type: 'image/jpeg' })
    const mockItems = [{ type: 'image/jpeg', getAsFile: () => file }]
    ;(wrapper.vm as any).handleWindowPaste({ clipboardData: { items: mockItems } })
    capturedOnload?.({ target: { result: 'data:image/jpeg;base64,pasteddata' } })
    await wrapper.vm.$nextTick()

    expect((wrapper.vm as any).showImage).toBe(true)
    expect((wrapper.vm as any).imageData).toBe('pasteddata')
  })

  it('handleWindowPaste ignores non-image clipboard items', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    const mockItems = [{ type: 'text/plain', getAsFile: () => null }]
    ;(wrapper.vm as any).handleWindowPaste({ clipboardData: { items: mockItems } })
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).imageData).toBeUndefined()
  })

  it('handleWindowPaste does nothing when clipboardData is absent', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps },
      global: { plugins: [vuetify] },
    })
    ;(wrapper.vm as any).handleWindowPaste({ clipboardData: null })
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).imageData).toBeUndefined()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })
})
