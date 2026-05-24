import { mount } from '@vue/test-utils'
import { describe, expect, it, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import DiaryTimeline from '@/components/DiaryTimeline.vue'
import DiaryEntry from '@/services/models/diaryEntry'

vi.mock('leaflet', () => ({
  default: {
    map: vi.fn(() => ({ setView: vi.fn().mockReturnThis(), remove: vi.fn() })),
    tileLayer: vi.fn(() => ({ addTo: vi.fn().mockReturnThis() })),
    marker: vi.fn(() => ({ addTo: vi.fn().mockReturnThis() })),
    Icon: { Default: { prototype: {}, mergeOptions: vi.fn() } },
  },
}))

const vuetify = createVuetify({ components, directives })

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
} = {}): DiaryEntry {
  const { location = 'London', entry = 'A lovely day.', date = new Date('2024-06-15T10:30:00'), ...options } = overrides
  return new DiaryEntry('diary-1', date, location, entry, { diaryEntryId: 'entry-1', ...options })
}

function mountTimeline (entries: DiaryEntry[], canEdit = false) {
  return mount(DiaryTimeline, {
    props: { entries, canEdit },
    global: { plugins: [vuetify] },
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
    const wrapper = mountTimeline([makeEntry({ date: new Date('2024-06-15T14:45:00') })])
    expect(wrapper.text()).toContain('14:45')
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
})
