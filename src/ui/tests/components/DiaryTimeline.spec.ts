import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import DiaryTimeline from '@/components/DiaryTimeline.vue'
import DiaryEntry, { type DiaryEntryImage } from '@/services/models/diaryEntry'

vi.mock('leaflet', () => ({
  default: {
    map: vi.fn(() => ({ setView: vi.fn().mockReturnThis(), remove: vi.fn() })),
    tileLayer: vi.fn(() => ({ addTo: vi.fn().mockReturnThis() })),
    marker: vi.fn(() => ({ addTo: vi.fn().mockReturnThis() })),
    Icon: { Default: { prototype: {}, mergeOptions: vi.fn() } },
  },
}))

const vuetify = createVuetify({ components, directives })

// Each viewer is teleported to the document; unmounting after every test takes it away again.
enableAutoUnmount(afterEach)

globalThis.ResizeObserver = require('resize-observer-polyfill')

function makeEntry (overrides: {
  location?: string
  entry?: string
  date?: Date
  diaryEntryId?: string
  mapLocation?: string
  showMap?: boolean
  fromLocation?: string
  toLocation?: string
  showJourney?: boolean
  images?: DiaryEntryImage[]
} = {}): DiaryEntry {
  const { location = 'London', entry = 'A lovely day.', date = new Date('2024-06-15T10:30:00'), ...options } = overrides
  return new DiaryEntry('diary-1', date, location, entry, { diaryEntryId: 'entry-1', ...options })
}

// <script setup> bindings are reachable at runtime but not in the component's public type,
// so the viewer's state is read through this shape rather than casting at every use.
type TimelineInternals = {
  zoomedSrc?: string
  zoomedCaption: string
  zoomedIndex: number
  stepImage: (by: number) => void
  openImage: (entry: DiaryEntry, index: number) => void
  openMap: (entry: DiaryEntry) => void
  mapDialog: boolean
  zoomedMapCaption: string
}

const jpeg = (data: string): DiaryEntryImage => ({ data, contentType: 'image/jpeg' })

function viewerState (wrapper: { vm: unknown }) {
  return wrapper.vm as TimelineInternals
}

// The viewer is a teleported dialog; tests that drive its controls attach to the document.
function mountTimeline (entries: DiaryEntry[], canEdit = false, attach = false) {
  return mount(DiaryTimeline, {
    props: { entries, canEdit },
    global: { plugins: [vuetify] },
    ...(attach ? { attachTo: document.body } : {}),
  })
}

describe('DiaryTimeline.vue', () => {
  it('renders a timeline item for each entry', () => {
    const entries = [
      makeEntry({ diaryEntryId: 'e1', location: 'Paris' }),
      makeEntry({ diaryEntryId: 'e2', location: 'Berlin' }),
    ]
    const wrapper = mountTimeline(entries)
    expect(wrapper.text()).toContain('Paris')
    expect(wrapper.text()).toContain('Berlin')
  })

  it('renders the entry text', () => {
    const wrapper = mountTimeline([makeEntry({ entry: 'Climbed the Eiffel Tower today.' })])
    expect(wrapper.text()).toContain('Climbed the Eiffel Tower today.')
  })

  it('formats the time in the opposite slot', () => {
    const wrapper = mountTimeline([makeEntry({ date: new Date('2024-06-15T14:45:00Z') })])
    expect(wrapper.text()).toContain('14:45')
  })

  it('shows the time the diarist wrote, whatever zone the viewer is in', () => {
    // stored wall clock: an entry written at 08:20 reads 08:20 in Kent and in Kenya alike
    const wrapper = mountTimeline([makeEntry({ date: new Date('1918-05-21T08:20:00Z') })])
    expect(wrapper.text()).toContain('08:20')
  })

  it('keeps a late evening entry on its own day', () => {
    const wrapper = mountTimeline([makeEntry({ date: new Date('1918-11-11T23:50:00Z') })])
    expect(wrapper.text()).toContain('23:50')
  })

  it('does not show edit or delete buttons when canEdit is false', () => {
    const wrapper = mountTimeline([makeEntry()], false)
    const buttons = wrapper.findAllComponents({ name: 'VBtn' })
    const editBtn = buttons.find(b => b.attributes('aria-label') === 'Edit entry')
    const deleteBtn = buttons.find(b => b.attributes('aria-label') === 'Delete entry')
    expect(editBtn).toBeUndefined()
    expect(deleteBtn).toBeUndefined()
  })

  it('shows edit and delete buttons when canEdit is true', () => {
    const wrapper = mountTimeline([makeEntry()], true)
    const buttons = wrapper.findAllComponents({ name: 'VBtn' })
    expect(buttons.some(b => b.attributes('aria-label') === 'Edit entry')).toBe(true)
    expect(buttons.some(b => b.attributes('aria-label') === 'Delete entry')).toBe(true)
  })

  it('emits edit with the entry when edit button is clicked', async () => {
    const entry = makeEntry({ location: 'Rome' })
    const wrapper = mountTimeline([entry], true)
    const editBtn = wrapper.findAllComponents({ name: 'VBtn' })
      .find(b => b.attributes('aria-label') === 'Edit entry')
    await editBtn!.trigger('click')
    expect(wrapper.emitted('edit')).toHaveLength(1)
    expect((wrapper.emitted('edit')![0][0] as DiaryEntry).location).toBe('Rome')
  })

  it('emits delete with the entry when delete button is clicked', async () => {
    const entry = makeEntry({ location: 'Rome' })
    const wrapper = mountTimeline([entry], true)
    const deleteBtn = wrapper.findAllComponents({ name: 'VBtn' })
      .find(b => b.attributes('aria-label') === 'Delete entry')
    await deleteBtn!.trigger('click')
    expect(wrapper.emitted('delete')).toHaveLength(1)
    expect((wrapper.emitted('delete')![0][0] as DiaryEntry).location).toBe('Rome')
  })

  it('renders no timeline items when entries array is empty', () => {
    const wrapper = mountTimeline([])
    expect(wrapper.findAllComponents({ name: 'VTimelineItem' })).toHaveLength(0)
  })

  it('does not render MapView when showMap is false', () => {
    const entry = makeEntry({ showMap: false, mapLocation: 'London, UK' })
    const wrapper = mountTimeline([entry])
    expect(wrapper.findComponent({ name: 'MapView' }).exists()).toBe(false)
  })

  it('does not render JourneyView when showJourney is false', () => {
    const entry = makeEntry({ showJourney: false, fromLocation: 'London', toLocation: 'Paris' })
    const wrapper = mountTimeline([entry])
    expect(wrapper.findComponent({ name: 'JourneyView' }).exists()).toBe(false)
  })

  it('renders MapView with the location when showMap is true', () => {
    const entry = makeEntry({ showMap: true, mapLocation: 'London, UK' })
    const wrapper = mountTimeline([entry])
    const map = wrapper.findComponent({ name: 'MapView' })
    expect(map.exists()).toBe(true)
    expect(map.props('location')).toBe('London, UK')
  })

  it('renders JourneyView with both endpoints when showJourney is true', () => {
    const entry = makeEntry({ showJourney: true, fromLocation: 'London', toLocation: 'Paris' })
    const wrapper = mountTimeline([entry])
    const journey = wrapper.findComponent({ name: 'JourneyView' })
    expect(journey.exists()).toBe(true)
    expect(journey.props('fromLocation')).toBe('London')
    expect(journey.props('toLocation')).toBe('Paris')
  })

  it('does not render the map column when the entry has no map or journey', () => {
    const wrapper = mountTimeline([makeEntry()])
    expect(wrapper.find('.entry-map-col').exists()).toBe(false)
    expect(wrapper.find('.entry-content--with-map').exists()).toBe(false)
  })

  it('lays out the entry with a map column when a map is shown', () => {
    const entry = makeEntry({ showMap: true, mapLocation: 'London, UK' })
    const wrapper = mountTimeline([entry])
    expect(wrapper.find('.entry-map-col').exists()).toBe(true)
    expect(wrapper.find('.entry-content--with-map').exists()).toBe(true)
  })

  it('lays out the entry with a map column when only a journey is shown', () => {
    // The wrapper condition and the JourneyView condition are separate expressions; this
    // is the case that catches them disagreeing, since the map half is entirely absent.
    const entry = makeEntry({ showJourney: true, fromLocation: 'London', toLocation: 'Paris' })
    const wrapper = mountTimeline([entry])
    expect(wrapper.find('.entry-map-col').exists()).toBe(true)
    expect(wrapper.find('.entry-content--with-map').exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'MapView' }).exists()).toBe(false)
  })

  it('renders both views when the entry has a map and a journey', () => {
    const entry = makeEntry({
      showMap: true,
      mapLocation: 'London, UK',
      showJourney: true,
      fromLocation: 'London',
      toLocation: 'Paris',
    })
    const wrapper = mountTimeline([entry])
    expect(wrapper.findComponent({ name: 'MapView' }).exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'JourneyView' }).exists()).toBe(true)
    expect(wrapper.findAll('.entry-map-col')).toHaveLength(1)
  })

  it('does not render the map column when showMap is set but the location is missing', () => {
    const entry = makeEntry({ showMap: true })
    const wrapper = mountTimeline([entry])
    expect(wrapper.findComponent({ name: 'MapView' }).exists()).toBe(false)
    expect(wrapper.find('.entry-map-col').exists()).toBe(false)
  })

  it('renders multiple entries with independent edit buttons', async () => {
    const entries = [
      makeEntry({ diaryEntryId: 'e1', location: 'Tokyo' }),
      makeEntry({ diaryEntryId: 'e2', location: 'Kyoto' }),
    ]
    const wrapper = mountTimeline(entries, true)
    const editBtns = wrapper.findAllComponents({ name: 'VBtn' })
      .filter(b => b.attributes('aria-label') === 'Edit entry')
    expect(editBtns).toHaveLength(2)

    await editBtns[1].trigger('click')
    expect((wrapper.emitted('edit')![0][0] as DiaryEntry).location).toBe('Kyoto')
  })

  it('does not render an image when the entry has none', () => {
    const wrapper = mountTimeline([makeEntry()])
    expect(wrapper.find('.diary-entry-media').exists()).toBe(false)
    expect(wrapper.find('.entry-thumbs').exists()).toBe(false)
  })

  it('renders the entry image from its base64 data', () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUJD')] })])
    const img = wrapper.findComponent({ name: 'VImg' })
    expect(img.exists()).toBe(true)
    expect(img.props('src')).toBe('data:image/jpeg;base64,QUJD')
  })

  it('shows no thumbnail row for a single image', () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUJD')] })])
    expect(wrapper.find('.entry-thumbs').exists()).toBe(false)
  })

  it('shows the first image large and the rest as thumbnails', () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUFB'), jpeg('QkJC'), jpeg('Q0ND')] })])
    const imgs = wrapper.findAllComponents({ name: 'VImg' })
    expect(imgs.map(i => i.props('src'))).toEqual([
      'data:image/jpeg;base64,QUFB',
      'data:image/jpeg;base64,QkJC',
      'data:image/jpeg;base64,Q0ND',
    ])
    expect(wrapper.findAll('.entry-thumb')).toHaveLength(2)
  })

  it('numbers each image in its alt text when there are several', () => {
    const wrapper = mountTimeline([makeEntry({
      date: new Date('1918-08-21T10:00:00'),
      images: [jpeg('QUFB'), jpeg('QkJC')],
    })])
    const alts = wrapper.findAllComponents({ name: 'VImg' }).map(i => i.props('alt'))
    expect(alts[0]).toContain('21 August 1918 (1 of 2)')
    expect(alts[1]).toContain('(2 of 2)')
  })

  it('does not number the alt text of a lone image', () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUJD')] })])
    expect(wrapper.findComponent({ name: 'VImg' }).props('alt')).not.toMatch(/\(\d+ of \d+\)/)
  })

  it('opens the full size viewer when the image is clicked', async () => {
    const wrapper = mountTimeline([makeEntry({
      location: 'Zanzibar',
      date: new Date('1918-08-21T10:00:00'),
      images: [jpeg('QUJD')],
    })])

    expect(wrapper.findComponent({ name: 'VDialog' }).props('modelValue')).toBe(false)

    await wrapper.find('.diary-entry-media--zoomable').trigger('click')

    expect(wrapper.findComponent({ name: 'VDialog' }).props('modelValue')).toBe(true)
    expect(viewerState(wrapper).zoomedCaption).toBe('Zanzibar — 21 August 1918')
    expect(viewerState(wrapper).zoomedSrc).toBe('data:image/jpeg;base64,QUJD')
  })

  it('opens the viewer from the keyboard', async () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUJD')] })])
    await wrapper.find('.diary-entry-media--zoomable').trigger('keydown.enter')
    expect(wrapper.findComponent({ name: 'VDialog' }).props('modelValue')).toBe(true)
  })

  it('opens a thumbnail at its own image', async () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUFB'), jpeg('QkJC'), jpeg('Q0ND')] })])

    await wrapper.findAll('.entry-thumb')[1].trigger('click')

    expect(viewerState(wrapper).zoomedIndex).toBe(2)
    expect(viewerState(wrapper).zoomedSrc).toBe('data:image/jpeg;base64,Q0ND')
  })

  it('opens a thumbnail from the keyboard', async () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUFB'), jpeg('QkJC')] })])
    await wrapper.find('.entry-thumb').trigger('keydown.space')
    expect(viewerState(wrapper).zoomedSrc).toBe('data:image/jpeg;base64,QkJC')
  })

  it('steps through an entry\'s images and stops at either end', async () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUFB'), jpeg('QkJC')] })])
    await wrapper.find('.diary-entry-media--zoomable').trigger('click')
    const vm = viewerState(wrapper)

    vm.stepImage(-1)
    expect(vm.zoomedIndex).toBe(0)
    vm.stepImage(1)
    expect(vm.zoomedSrc).toBe('data:image/jpeg;base64,QkJC')
    vm.stepImage(1)
    expect(vm.zoomedIndex).toBe(1)
  })

  it('pages with the viewer buttons and the arrow keys, showing the position', async () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUFB'), jpeg('QkJC'), jpeg('Q0ND')] })], false, true)
    await wrapper.find('.diary-entry-media--zoomable').trigger('click')
    await flushPromises()
    const viewer = () => document.querySelector('.image-viewer') as HTMLElement

    expect(viewer().textContent).toContain('1 of 3')
    ;(document.querySelector('[aria-label="Next photograph"]') as HTMLElement).click()
    await flushPromises()
    expect(viewer().textContent).toContain('2 of 3')

    viewer().dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }))
    await flushPromises()
    expect(viewerState(wrapper).zoomedIndex).toBe(2)

    viewer().dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }))
    ;(document.querySelector('[aria-label="Previous photograph"]') as HTMLElement).click()
    await flushPromises()
    expect(viewerState(wrapper).zoomedIndex).toBe(0)
  })

  it('shows no paging controls for a single image', async () => {
    const wrapper = mountTimeline([makeEntry({ images: [jpeg('QUJD')] })], false, true)
    await wrapper.find('.diary-entry-media--zoomable').trigger('click')
    await flushPromises()
    expect(document.querySelector('[aria-label="Next photograph"]')).toBeNull()
  })

  it('shows the clicked entry image when several entries have one', async () => {
    const wrapper = mountTimeline([
      makeEntry({ diaryEntryId: 'e1', location: 'Lindi', images: [jpeg('QUFB')] }),
      makeEntry({ diaryEntryId: 'e2', location: 'Kilwa', images: [jpeg('QkJC')] }),
    ])

    await wrapper.findAll('.diary-entry-media--zoomable')[1].trigger('click')

    expect(viewerState(wrapper).zoomedSrc).toBe('data:image/jpeg;base64,QkJC')
    expect(viewerState(wrapper).zoomedCaption).toContain('Kilwa')
  })

  it('does not open the viewer for an image that is not there', () => {
    const entry = makeEntry({ images: [jpeg('QUFB')] })
    const wrapper = mountTimeline([entry])
    viewerState(wrapper).openImage(entry, 5)
    expect(wrapper.findComponent({ name: 'VDialog' }).props('modelValue')).toBe(false)
  })

  it('opens the map full size, and interactive, when it is clicked', async () => {
    const wrapper = mountTimeline([makeEntry({ location: 'Lumbo', showMap: true, mapLocation: 'Lumbo, Mozambique', date: new Date('1918-08-02T13:00:00') })])
    const inline = wrapper.findComponent({ name: 'MapView' })
    expect(inline.props('interactive')).toBeFalsy()

    await wrapper.find('.entry-map').trigger('click')

    expect(viewerState(wrapper).mapDialog).toBe(true)
    expect(viewerState(wrapper).zoomedMapCaption).toBe('Lumbo — 2 August 1918')
    const views = wrapper.findAllComponents({ name: 'MapView' })
    expect(views).toHaveLength(2)
    expect(views[1].props()).toMatchObject({ interactive: true, height: '70dvh', location: 'Lumbo, Mozambique' })
  })

  it('opens a journey full size from the keyboard', async () => {
    const wrapper = mountTimeline([makeEntry({ showJourney: true, fromLocation: 'Lindi', toLocation: 'Lumbo' })])
    await wrapper.find('.entry-map').trigger('keydown.enter')
    const views = wrapper.findAllComponents({ name: 'JourneyView' })
    expect(views).toHaveLength(2)
    expect(views[1].props()).toMatchObject({ interactive: true, fromLocation: 'Lindi', toLocation: 'Lumbo' })
  })

  it('names the map by its entry for assistive technology', () => {
    const wrapper = mountTimeline([makeEntry({ location: 'Lumbo', showMap: true, mapLocation: 'Lumbo, Mozambique' })])
    expect(wrapper.find('.entry-map').attributes('aria-label')).toBe('Open the map for Lumbo full size')
  })

  it('does not open the map panel for an entry without a map', () => {
    const entry = makeEntry()
    const wrapper = mountTimeline([entry])
    viewerState(wrapper).openMap(entry)
    expect(viewerState(wrapper).mapDialog).toBe(false)
  })
})
