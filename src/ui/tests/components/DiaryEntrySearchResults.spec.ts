import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import DiaryEntrySearchResults from '@/components/DiaryEntrySearchResults.vue'
import DiaryEntry from '@/services/models/diaryEntry'
import type PagedResult from '@/services/models/pagedResult'

const vuetify = createVuetify({ components, directives })

globalThis.ResizeObserver = require('resize-observer-polyfill')

function makeEntry (overrides: {
  location?: string
  entry?: string
  date?: Date
  diaryEntryId?: string
} = {}): DiaryEntry {
  const { location = 'London', entry = 'A lovely day in the city', date = new Date('2024-06-15T10:30:00'), ...options } = overrides
  return new DiaryEntry('diary-1', date, location, entry, { diaryEntryId: 'entry-1', ...options })
}

function makeResults (items: DiaryEntry[], totalCount?: number): PagedResult<DiaryEntry> {
  return { items, totalCount: totalCount ?? items.length, page: 1, pageSize: 10 }
}

function mountResults (props: Partial<InstanceType<typeof DiaryEntrySearchResults>['$props']> = {}) {
  return mount(DiaryEntrySearchResults, {
    props: {
      search: 'london',
      loading: false,
      results: null,
      page: 1,
      pageSize: 10,
      ...props,
    },
    global: { plugins: [vuetify] },
  })
}

describe('DiaryEntrySearchResults.vue', () => {
  it('shows progress bar when loading is true', () => {
    const wrapper = mountResults({ loading: true })
    const progress = wrapper.findComponent({ name: 'VProgressLinear' })
    expect(progress.exists()).toBe(true)
    expect(progress.props('active')).toBe(true)
  })

  it('hides progress bar when loading is false', () => {
    const wrapper = mountResults({ loading: false })
    const progress = wrapper.findComponent({ name: 'VProgressLinear' })
    expect(progress.props('active')).toBe(false)
  })

  it('shows no-results message when totalCount is 0', () => {
    const wrapper = mountResults({
      loading: false,
      results: makeResults([], 0),
      search: 'xyz123',
    })
    expect(wrapper.text()).toContain('No entries found for "xyz123"')
  })

  it('renders a list item for each result', () => {
    const entries = [
      makeEntry({ diaryEntryId: 'e1', location: 'Paris' }),
      makeEntry({ diaryEntryId: 'e2', location: 'Berlin' }),
    ]
    const wrapper = mountResults({ results: makeResults(entries) })
    const items = wrapper.findAllComponents({ name: 'VListItem' })
    expect(items).toHaveLength(2)
    expect(wrapper.text()).toContain('Paris')
    expect(wrapper.text()).toContain('Berlin')
  })

  it('shows entry text in subtitle', () => {
    const wrapper = mountResults({
      results: makeResults([makeEntry({ entry: 'Visited the Eiffel Tower' })]),
    })
    expect(wrapper.text()).toContain('Visited the Eiffel Tower')
  })

  it('formats the date correctly in the prepend slot', () => {
    const wrapper = mountResults({
      results: makeResults([makeEntry({ date: new Date('2024-06-15T10:30:00') })]),
    })
    expect(wrapper.text()).toContain('Sat 15 Jun 2024')
    expect(wrapper.text()).toContain('10:30')
  })

  it('emits select with the entry when a list item is clicked', async () => {
    const entry = makeEntry({ location: 'Madrid' })
    const wrapper = mountResults({ results: makeResults([entry]) })
    await wrapper.findComponent({ name: 'VListItem' }).trigger('click')
    expect(wrapper.emitted('select')).toHaveLength(1)
    expect((wrapper.emitted('select')![0][0] as DiaryEntry).location).toBe('Madrid')
  })

  it('shows pagination when totalCount exceeds pageSize', () => {
    const entries = Array.from({ length: 5 }, (_, i) =>
      makeEntry({ diaryEntryId: `e${i}`, location: `City ${i}` })
    )
    const wrapper = mountResults({
      results: makeResults(entries, 25),
      pageSize: 5,
    })
    expect(wrapper.findComponent({ name: 'VPagination' }).exists()).toBe(true)
  })

  it('does not show pagination when all results fit on one page', () => {
    const entries = [makeEntry()]
    const wrapper = mountResults({
      results: makeResults(entries, 1),
      pageSize: 10,
    })
    expect(wrapper.findComponent({ name: 'VPagination' }).exists()).toBe(false)
  })

  it('emits update:page when pagination changes', async () => {
    const entries = Array.from({ length: 5 }, (_, i) =>
      makeEntry({ diaryEntryId: `e${i}` })
    )
    const wrapper = mountResults({
      results: makeResults(entries, 25),
      pageSize: 5,
    })
    const pagination = wrapper.findComponent({ name: 'VPagination' })
    await pagination.vm.$emit('update:modelValue', 2)
    expect(wrapper.emitted('update:page')).toHaveLength(1)
    expect(wrapper.emitted('update:page')![0][0]).toBe(2)
  })

  it('renders nothing when results is null and not loading', () => {
    const wrapper = mountResults({ results: null, loading: false })
    expect(wrapper.findComponent({ name: 'VList' }).exists()).toBe(false)
    expect(wrapper.findComponent({ name: 'VPagination' }).exists()).toBe(false)
  })
})
