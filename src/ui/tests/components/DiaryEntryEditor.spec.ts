import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import DiaryEntryEditor from '@/components/DiaryEntryEditor.vue'
import { MAX_ENTRY_IMAGES } from '@/services/models/diaryEntry'
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
    journeyMode: 'crow-flies' as const,
  }

  it('renders form fields with initial props', () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: defaultProps,
      global: { plugins: [vuetify] },
    })
    expect((wrapper.find('#location').element as HTMLInputElement).value).toBe('Kitchen')
    expect((wrapper.find('#entry').element as HTMLInputElement).value).toBe('Cooked pasta')
    expect((wrapper.vm as any).time).toBe('12:30')
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
    const payload = emitted![0][0] as { showJourney: boolean; fromLocation: string; toLocation: string; journeyMode: string }
    expect(payload.showJourney).toBe(true)
    expect(payload.fromLocation).toBe('Sandwich, UK')
    expect(payload.toLocation).toBe('Southampton, UK')
    expect(payload.journeyMode).toBe('crow-flies')
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
    const payload = emitted![0][0] as { showJourney: boolean; fromLocation: string; toLocation: string; journeyMode: string }
    expect(payload.showJourney).toBe(false)
    expect(payload.fromLocation).toBe('')
    expect(payload.toLocation).toBe('')
    expect(payload.journeyMode).toBe('crow-flies')
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

  // Reads each file as `data:<type>;base64,<file name>`, answering asynchronously like the real one.
  function mockFileReader () {
    vi.spyOn(globalThis, 'FileReader').mockImplementation(() => {
      const reader: any = {
        readAsDataURL (file: File) {
          queueMicrotask(() => reader.onload?.({ target: { result: `data:${file.type};base64,${file.name}` } }))
        },
      }
      return reader
    })
  }

  const jpeg = (data: string) => ({ data, contentType: 'image/jpeg' })

  function mountEditor (props: Record<string, unknown> = {}) {
    return mount(DiaryEntryEditor, {
      props: { ...defaultProps, ...props },
      global: { plugins: [vuetify] },
    })
  }

  async function submitted (wrapper: ReturnType<typeof mountEditor>) {
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    return emitted![0][0] as { images: { data: string, contentType: string }[] }
  }

  it('does not show image drop zone when showImage is false', () => {
    const wrapper = mountEditor()
    expect(wrapper.find('#image-drop-zone').exists()).toBe(false)
  })

  it('shows image drop zone when showImage is toggled on', async () => {
    const wrapper = mountEditor()
    ;(wrapper.vm as any).showImage = true
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#image-drop-zone').exists()).toBe(true)
  })

  it('shows the image section and a tile per image when images are provided', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#image-drop-zone').exists()).toBe(true)
    expect(wrapper.findAll('.image-tile')).toHaveLength(2)
  })

  it('lets the file input choose several files', async () => {
    const wrapper = mountEditor({ images: [jpeg('a')] })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#diary-entry-image-input').attributes('multiple')).toBeDefined()
  })

  it('clears images when showImage is toggled off', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    ;(wrapper.vm as any).showImage = false
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).images).toEqual([])
  })

  it('emits submit with its images in order', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    const payload = await submitted(wrapper)
    expect(payload.images).toEqual([jpeg('a'), jpeg('b')])
  })

  it('emits submit with no images by default', async () => {
    const wrapper = mountEditor()
    const payload = await submitted(wrapper)
    expect(payload.images).toEqual([])
  })

  it('emits a copy, so later edits do not reach the submitted payload', async () => {
    const wrapper = mountEditor({ images: [jpeg('a')] })
    const payload = await submitted(wrapper)
    ;(wrapper.vm as any).removeImage(0)
    expect(payload.images).toEqual([jpeg('a')])
  })

  it('updates images when the prop changes', async () => {
    const wrapper = mountEditor({ images: [jpeg('a')] })
    await wrapper.setProps({ images: [jpeg('x'), jpeg('y')] })
    expect((wrapper.vm as any).images).toEqual([jpeg('x'), jpeg('y')])
  })

  it('treats a missing images prop as none', async () => {
    const wrapper = mountEditor({ images: [jpeg('a')] })
    await wrapper.setProps({ images: undefined })
    expect((wrapper.vm as any).images).toEqual([])
  })

  it('clearImages removes every image and hides the section', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    ;(wrapper.vm as any).clearImages()
    await wrapper.vm.$nextTick()
    expect((wrapper.vm as any).images).toEqual([])
    expect((wrapper.vm as any).showImage).toBe(false)
  })

  it('removeImage removes only that image', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b'), jpeg('c')] })
    ;(wrapper.vm as any).removeImage(1)
    expect((wrapper.vm as any).images).toEqual([jpeg('a'), jpeg('c')])
  })

  it('the remove button on a tile removes that image', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    await wrapper.vm.$nextTick()
    await wrapper.find('[aria-label="Remove image 1"]').trigger('click')
    expect((wrapper.vm as any).images).toEqual([jpeg('b')])
  })

  it('moveImage moves an image earlier or later', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b'), jpeg('c')] })
    ;(wrapper.vm as any).moveImage(2, -1)
    expect((wrapper.vm as any).images).toEqual([jpeg('a'), jpeg('c'), jpeg('b')])
    ;(wrapper.vm as any).moveImage(0, 1)
    expect((wrapper.vm as any).images).toEqual([jpeg('c'), jpeg('a'), jpeg('b')])
  })

  it('moveImage ignores a move past either end', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    ;(wrapper.vm as any).moveImage(0, -1)
    ;(wrapper.vm as any).moveImage(1, 1)
    expect((wrapper.vm as any).images).toEqual([jpeg('a'), jpeg('b')])
  })

  it('disables moving the first image earlier and the last later', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('[aria-label="Move image 1 earlier"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[aria-label="Move image 2 later"]').attributes('disabled')).toBeDefined()
    expect(wrapper.find('[aria-label="Move image 1 later"]').attributes('disabled')).toBeUndefined()
  })

  it('the move buttons on a tile reorder the images', async () => {
    const wrapper = mountEditor({ images: [jpeg('a'), jpeg('b')] })
    await wrapper.vm.$nextTick()
    await wrapper.find('[aria-label="Move image 1 later"]').trigger('click')
    expect((wrapper.vm as any).images).toEqual([jpeg('b'), jpeg('a')])
  })

  it('triggerFileInput clicks the file input element', async () => {
    const wrapper = mountEditor()
    ;(wrapper.vm as any).showImage = true
    await wrapper.vm.$nextTick()
    const clickSpy = vi.fn()
    ;(wrapper.vm as any).fileInputRef = { click: clickSpy }
    ;(wrapper.vm as any).triggerFileInput()
    expect(clickSpy).toHaveBeenCalled()
  })

  it('Enter key on drop zone triggers file input', async () => {
    const wrapper = mountEditor()
    ;(wrapper.vm as any).showImage = true
    await wrapper.vm.$nextTick()
    const clickSpy = vi.fn()
    ;(wrapper.vm as any).fileInputRef = { click: clickSpy }
    await wrapper.find('#image-drop-zone').trigger('keydown.enter')
    expect(clickSpy).toHaveBeenCalled()
  })

  it('handleDrop adds every dropped image, in order, after the existing ones', async () => {
    mockFileReader()
    const wrapper = mountEditor({ images: [jpeg('a')] })
    ;(wrapper.vm as any).handleDrop({
      dataTransfer: {
        files: [
          new File(['1'], 'one', { type: 'image/png' }),
          new File(['2'], 'two', { type: 'image/jpeg' }),
        ],
      },
    })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([
      jpeg('a'),
      { data: 'one', contentType: 'image/png' },
      { data: 'two', contentType: 'image/jpeg' },
    ])
    expect((wrapper.vm as any).isDragging).toBe(false)
  })

  it('handleDrop ignores non-image files', async () => {
    mockFileReader()
    const wrapper = mountEditor()
    ;(wrapper.vm as any).handleDrop({ dataTransfer: { files: [new File(['x'], 'notes', { type: 'text/plain' })] } })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([])
    expect((wrapper.vm as any).showImage).toBe(false)
  })

  it('handleDrop copes with a drop that carries no files', async () => {
    const wrapper = mountEditor()
    ;(wrapper.vm as any).handleDrop({ dataTransfer: null })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([])
  })

  it('handleFileSelect adds the selected images and resets the input', async () => {
    mockFileReader()
    const wrapper = mountEditor()
    const input = { files: [new File(['1'], 'chosen', { type: 'image/jpeg' })], value: 'C:\\fakepath\\chosen' }
    ;(wrapper.vm as any).handleFileSelect({ target: input })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([jpeg('chosen')])
    expect((wrapper.vm as any).showImage).toBe(true)
    expect(input.value).toBe('')
  })

  it('handleFileSelect copes with an input that has no files', async () => {
    const wrapper = mountEditor()
    ;(wrapper.vm as any).handleFileSelect({ target: { files: null, value: '' } })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([])
  })

  it('never adds more than the limit, keeping the earliest files', async () => {
    mockFileReader()
    const existing = Array.from({ length: MAX_ENTRY_IMAGES - 1 }, (_, i) => jpeg(`old${i}`))
    const wrapper = mountEditor({ images: existing })
    ;(wrapper.vm as any).handleDrop({
      dataTransfer: {
        files: [
          new File(['1'], 'first', { type: 'image/jpeg' }),
          new File(['2'], 'second', { type: 'image/jpeg' }),
        ],
      },
    })
    await flushPromises()
    const images = (wrapper.vm as any).images
    expect(images).toHaveLength(MAX_ENTRY_IMAGES)
    expect(images.at(-1)).toEqual(jpeg('first'))
  })

  it('hides the drop zone and explains the limit once it is reached', async () => {
    const full = Array.from({ length: MAX_ENTRY_IMAGES }, (_, i) => jpeg(`img${i}`))
    const wrapper = mountEditor({ images: full })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#image-drop-zone').exists()).toBe(false)
    expect(wrapper.text()).toContain(`up to ${MAX_ENTRY_IMAGES} images`)
  })

  it('ignores files dropped when already full', async () => {
    mockFileReader()
    const full = Array.from({ length: MAX_ENTRY_IMAGES }, (_, i) => jpeg(`img${i}`))
    const wrapper = mountEditor({ images: full })
    ;(wrapper.vm as any).handleDrop({ dataTransfer: { files: [new File(['1'], 'extra', { type: 'image/jpeg' })] } })
    await flushPromises()
    expect((wrapper.vm as any).images).toHaveLength(MAX_ENTRY_IMAGES)
  })

  it('handleWindowPaste adds every pasted image and shows the image section', async () => {
    mockFileReader()
    const wrapper = mountEditor()
    const mockItems = [
      { type: 'image/jpeg', getAsFile: () => new File(['1'], 'pasted1', { type: 'image/jpeg' }) },
      { type: 'text/plain', getAsFile: () => null },
      { type: 'image/jpeg', getAsFile: () => new File(['2'], 'pasted2', { type: 'image/jpeg' }) },
    ]
    ;(wrapper.vm as any).handleWindowPaste({ clipboardData: { items: mockItems } })
    await flushPromises()
    expect((wrapper.vm as any).showImage).toBe(true)
    expect((wrapper.vm as any).images).toEqual([jpeg('pasted1'), jpeg('pasted2')])
  })

  it('handleWindowPaste skips an image item that yields no file', async () => {
    mockFileReader()
    const wrapper = mountEditor()
    ;(wrapper.vm as any).handleWindowPaste({ clipboardData: { items: [{ type: 'image/png', getAsFile: () => null }] } })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([])
  })

  it('handleWindowPaste ignores non-image clipboard items', async () => {
    const wrapper = mountEditor()
    const mockItems = [{ type: 'text/plain', getAsFile: () => null }]
    ;(wrapper.vm as any).handleWindowPaste({ clipboardData: { items: mockItems } })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([])
  })

  it('handleWindowPaste does nothing when clipboardData is absent', async () => {
    const wrapper = mountEditor()
    ;(wrapper.vm as any).handleWindowPaste({ clipboardData: null })
    await flushPromises()
    expect((wrapper.vm as any).images).toEqual([])
  })

  it('skips a file that fails to read and keeps the rest', async () => {
    vi.spyOn(globalThis, 'FileReader').mockImplementation(() => {
      const reader: any = {
        error: new Error('unreadable'),
        readAsDataURL (file: File) {
          queueMicrotask(() => file.name === 'bad'
            ? reader.onerror?.()
            : reader.onload?.({ target: { result: `data:${file.type};base64,${file.name}` } }))
        },
      }
      return reader
    })
    const wrapper = mountEditor()
    await (wrapper.vm as any).addFiles([
      new File(['1'], 'bad', { type: 'image/jpeg' }),
      new File(['2'], 'good', { type: 'image/jpeg' }),
    ])
    expect((wrapper.vm as any).images).toEqual([jpeg('good')])
  })

  it('does not show journey-mode selector when showJourney is false', () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: false },
      global: { plugins: [vuetify] },
    })
    expect(wrapper.find('#journey-mode').exists()).toBe(false)
  })

  it('shows journey-mode selector when showJourney is true', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true, fromLocation: 'Sandwich, UK', toLocation: 'Southampton, UK' },
      global: { plugins: [vuetify] },
    })
    await wrapper.vm.$nextTick()
    expect(wrapper.find('#journey-mode').exists()).toBe(true)
  })

  it('emits submit with selected journeyMode in payload', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true, fromLocation: 'Sandwich, UK', toLocation: 'Southampton, UK', journeyMode: 'car' as const },
      global: { plugins: [vuetify] },
    })
    const submitEventPromise = Promise.resolve({ valid: true })
    await (wrapper.vm as any).submit(submitEventPromise)
    await submitEventPromise
    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    const payload = emitted![0][0] as { journeyMode: string }
    expect(payload.journeyMode).toBe('car')
  })

  it('journey mode selector uses crow-flies as default journeyMode', async () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true, journeyMode: 'crow-flies' as const },
      global: { plugins: [vuetify] },
    })
    await wrapper.vm.$nextTick()
    const payload = { journeyMode: (wrapper.vm as any).journeyMode }
    expect(payload.journeyMode).toBe('crow-flies')
  })

  it('journey mode items include icons for each mode', () => {
    const wrapper = mount(DiaryEntryEditor, {
      props: { ...defaultProps, showJourney: true },
      global: { plugins: [vuetify] },
    })
    const items = (wrapper.vm as any).journeyModeItems as Array<{ label: string; value: string; icon: string }>
    expect(items).toHaveLength(5)
    for (const item of items) {
      expect(item.icon).toBeTruthy()
      expect(item.icon).toMatch(/^\$mdi-/)
    }
    expect(items.find(i => i.value === 'crow-flies')?.icon).toBe('$mdi-bird')
    expect(items.find(i => i.value === 'walking')?.icon).toBe('$mdi-walk')
    expect(items.find(i => i.value === 'car')?.icon).toBe('$mdi-car')
    expect(items.find(i => i.value === 'train')?.icon).toBe('$mdi-train')
    expect(items.find(i => i.value === 'boat')?.icon).toBe('$mdi-ferry')
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })
})
