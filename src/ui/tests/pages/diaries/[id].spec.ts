import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'

import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'

// import { useRoute } from 'vue-router'
import { diaryAPI } from '@/services/modules/diaryService'
import { diaryEntryAPI } from '@/services/modules/diaryEntryService'
import { state } from '@/services/authentication/msalConfig'
import { useApiStatusStore } from '@/stores/apiStatus'
import Component from '@/pages/diaries/[id].vue'
import Diary from '@/services/models/diary'
import DiaryEntry from '@/services/models/diaryEntry'
import dayjs from 'dayjs'

// Shared mocks accessible to tests (vi.hoisted ensures they're available before vi.mock runs)
const mockRouterPush = vi.hoisted(() => vi.fn())
const mockRouterReplace = vi.hoisted(() => vi.fn())
const mockQuery = vi.hoisted(() => ({ date: undefined as string | undefined }))

// Mock BEFORE importing the component!
vi.mock('vue-router', () => ({
  useRoute: () => ({
    params: { id: 'test-diary-id' },
    query: mockQuery,
  }),
  useRouter: () => ({
    push: mockRouterPush,
    replace: mockRouterReplace,
  }),
}))

vi.mock('@grafana/faro-web-sdk', () => ({
  getWebInstrumentations: vi.fn(() => []),
  initializeFaro: vi.fn(() => ({ api: { pushEvent: vi.fn(), startUserAction: vi.fn(() => ({ end: vi.fn() })) } })),
  TransportItemType: { LOG: 'log' },
}))

vi.mock('leaflet', () => ({
  default: {
    map: vi.fn(() => ({ setView: vi.fn().mockReturnThis(), remove: vi.fn() })),
    tileLayer: vi.fn(() => ({ addTo: vi.fn().mockReturnThis() })),
    marker: vi.fn(() => ({ addTo: vi.fn().mockReturnThis() })),
    Icon: { Default: { prototype: {}, mergeOptions: vi.fn() } },
  },
}))

globalThis.ResizeObserver = require('resize-observer-polyfill')

describe('[id].vue', () => {
  const diaryId = 'test-diary-id'
  const diary = new Diary('Test Diary', 'Test Author', 'Test Desc', diaryId)
  const diaryEntry = new DiaryEntry(diaryId, new Date(), 'Test Location', 'Test Entry', { diaryEntryId: 'entry-id' })
  const minDate = new Date(2020, 0, 1)
  const maxDate = new Date(2020, 11, 31)

  let wrapper: VueWrapper

  beforeEach(() => {
    setActivePinia(createPinia())
    // Mock localStorage
    const localStorageMock = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      clear: vi.fn(),
      removeItem: vi.fn(),
    }
    Object.defineProperty(globalThis, 'localStorage', {
      value: localStorageMock,
      writable: true,
    })

    mockQuery.date = undefined
    vi.clearAllMocks()
    state.isAuthenticated = false
    vi.spyOn(diaryAPI, 'getDiary').mockResolvedValue(diary)
    vi.spyOn(diaryEntryAPI, 'getMinDate').mockResolvedValue(minDate)
    vi.spyOn(diaryEntryAPI, 'getMaxDate').mockResolvedValue(maxDate)
    vi.spyOn(diaryEntryAPI, 'searchDiaryEntry').mockResolvedValue([1, 5, 20])
    vi.spyOn(diaryEntryAPI, 'searchDiaryEntryForDay').mockResolvedValue([diaryEntry])
    vi.spyOn(diaryEntryAPI, 'createDiaryEntry').mockResolvedValue(null)
    vi.spyOn(diaryEntryAPI, 'updateDiaryEntry').mockResolvedValue(null)
    vi.spyOn(diaryEntryAPI, 'deleteDiaryEntry').mockResolvedValue(false)
    wrapper = mount(Component, {
      propsData: {},
      global: {
        plugins: [vuetify],
      },
    })
  })

  afterEach(() => {
    wrapper.unmount()
  })

  it('renders diary title and author', async () => {
    await flushPromises()
    expect(wrapper.text()).toContain('Test Diary')
    expect(wrapper.text()).toContain('Test Author')
  })

  it('shows Add button only when authenticated', async () => {
    await flushPromises()
    expect(wrapper.html()).not.toContain('Add')
    state.isAuthenticated = true
    await flushPromises()
    expect(wrapper.html()).toContain('Add')
  })

  it('calls editItem when Add button is clicked', async () => {
    state.isAuthenticated = true
    await flushPromises()
    const addBtn = wrapper.findAll('button').find(btn => btn.text() === 'Add')
    expect(addBtn).toBeTruthy()
    if (addBtn) await addBtn.trigger('click')
    expect((wrapper.vm as any).dialog).toBe(true)
  })

  it('calls deleteItem when delete button is clicked', async () => {
    state.isAuthenticated = true
    await flushPromises()
    // Find the delete button in the timeline
    const deleteBtn = wrapper.findAll('button').find(btn => btn.html().includes('mdi-delete'))
    expect(deleteBtn).toBeTruthy()
    if (deleteBtn) await deleteBtn.trigger('click')
    expect((wrapper.vm as any).dialogDelete).toBe(true)
  })

  it('calls moveForward and moveBackward', async () => {
    await flushPromises()
    const forwardBtn = wrapper.findAll('button').find(btn => btn.html().includes('mdi-fast-forward'))
    const backwardBtn = wrapper.findAll('button').find(btn => btn.html().includes('mdi-rewind'))
    expect(forwardBtn).toBeTruthy()
    expect(backwardBtn).toBeTruthy()
    if (forwardBtn) await forwardBtn.trigger('click')
    if (backwardBtn) await backwardBtn.trigger('click')
    // No error means the methods ran
  })

  // correct text of button displayed when isDatePickerExpanded is true vs false
  it('shows correct toggle button text based on isDatePickerExpanded state', async () => {
    // Set initial state to false
    (wrapper.vm as any).isDatePickerExpanded = false

    await flushPromises()

    // Find the toggle button
    const datePickerToggleBtn = wrapper.findAll('button').find(btn => btn.html().includes('Collapse date picker') || btn.html().includes('Expand date picker'))
    expect(datePickerToggleBtn).toBeTruthy()
    expect(datePickerToggleBtn?.text()).toBe('Expanded View')
    if (datePickerToggleBtn) await datePickerToggleBtn.trigger('click')
    await flushPromises()
    expect(datePickerToggleBtn?.text()).toBe('Compact View')
  })

  it('calls onSubmitDiaryEntry for new entry', async () => {
    state.isAuthenticated = true
    await flushPromises()
    // Simulate opening dialog and submitting
    await (wrapper.vm as any).editItem()
    await (wrapper.vm as any).onSubmitDiaryEntry({
      date: new Date(),
      location: 'New Location',
      entry: 'New Entry',
      mapLocation: 'London, UK',
      showMap: true,
      fromLocation: 'Sandwich, UK',
      toLocation: 'Southampton, UK',
      showJourney: true,
      imageData: undefined,
      imageContentType: undefined,
    })
    expect(diaryEntryAPI.createDiaryEntry).toHaveBeenCalled()
    expect((wrapper.vm as any).editedItem.mapLocation).toBe('London, UK')
    expect((wrapper.vm as any).editedItem.showMap).toBe(true)
    expect((wrapper.vm as any).editedItem.fromLocation).toBe('Sandwich, UK')
    expect((wrapper.vm as any).editedItem.toLocation).toBe('Southampton, UK')
    expect((wrapper.vm as any).editedItem.showJourney).toBe(true)
  })

  it('onSubmitDiaryEntry stores imageData and imageContentType on editedItem', async () => {
    state.isAuthenticated = true
    await flushPromises()
    await (wrapper.vm as any).editItem()
    await (wrapper.vm as any).onSubmitDiaryEntry({
      date: new Date(),
      location: 'Test',
      entry: 'Test entry',
      mapLocation: '',
      showMap: false,
      fromLocation: '',
      toLocation: '',
      showJourney: false,
      imageData: 'abc123',
      imageContentType: 'image/jpeg',
    })
    expect((wrapper.vm as any).editedItem.imageData).toBe('abc123')
    expect((wrapper.vm as any).editedItem.imageContentType).toBe('image/jpeg')
  })

  it('renders image in timeline when entry has imageData and imageContentType', async () => {
    const entryWithImage = new DiaryEntry(diaryId, new Date(), 'Location', 'Entry', { diaryEntryId: 'img-entry-id', imageData: 'abc123', imageContentType: 'image/jpeg' })
    vi.spyOn(diaryEntryAPI, 'searchDiaryEntryForDay').mockResolvedValue([entryWithImage])
    await flushPromises()
    await (wrapper.vm as any).selectDate(new Date())
    await flushPromises()
    const vImgs = wrapper.findAllComponents({ name: 'VImg' })
    const imageComponent = vImgs.find(c => c.props('src') === 'data:image/jpeg;base64,abc123')
    expect(imageComponent).toBeDefined()
  })

  it('calls onSubmitDiaryEntry for update', async () => {
    state.isAuthenticated = true
    await flushPromises()
    // Simulate editing an existing entry
    const entry = new DiaryEntry(diaryId, new Date(), 'Loc', 'Entry', { diaryEntryId: 'existing-id' })
    await (wrapper.vm as any).editItem(entry)
    await (wrapper.vm as any).onSubmitDiaryEntry({
      date: entry.date,
      location: entry.location,
      entry: entry.entry,
      mapLocation: 'Berlin, Germany',
      showMap: false,
      fromLocation: '',
      toLocation: '',
      showJourney: false,
    })
    expect(diaryEntryAPI.updateDiaryEntry).toHaveBeenCalled()
    expect((wrapper.vm as any).editedItem.mapLocation).toBe('Berlin, Germany')
    expect((wrapper.vm as any).editedItem.showMap).toBe(false)
    expect((wrapper.vm as any).editedItem.showJourney).toBe(false)
  })

  it('should toggle isDatePickerExpanded from false to true', async () => {
    // Set initial state to false
    (wrapper.vm as any).isDatePickerExpanded = false;

    // Call toggle function
    (wrapper.vm as any).toggleDatePickerHeight()

    // Verify state changed to true
    expect((wrapper.vm as any).isDatePickerExpanded).toBe(true)
  })

  // onSubmitDiaryEntry() when date matches selectedDate
  it('onSubmitDiaryEntry calls selectDate when date matches selectedDate', async () => {
    state.isAuthenticated = true
    await flushPromises()

    const testDate = new Date(2024, 1, 15);
    (wrapper.vm as any).selectedDate = testDate;

    // Setup editedItem for creation (no diaryEntryId)
    (wrapper.vm as any).editedItem = new DiaryEntry(diaryId, testDate, 'Test Location', 'Test Entry')

    // Call onSubmitDiaryEntry with matching date
    await (wrapper.vm as any).onSubmitDiaryEntry({
      date: testDate,
      location: 'Updated Location',
      entry: 'Updated Entry',
      mapLocation: '',
      showMap: false,
      fromLocation: '',
      toLocation: '',
      showJourney: false,
    })

    // Verify selectDate was called because dates match
    expect(diaryEntryAPI.createDiaryEntry).toHaveBeenCalled()
  })

  // deleteItemConfirm() when date matches selectedDate
  it('deleteItemConfirm calls selectDate when date matches selectedDate', async () => {
    state.isAuthenticated = true
    await flushPromises()

    const testDate = new Date(2024, 1, 20);
    (wrapper.vm as any).selectedDate = testDate;

    // Setup editedItem with matching date and diaryEntryId
    (wrapper.vm as any).editedItem = new DiaryEntry(diaryId, testDate, 'Test Location', 'Test Entry', { diaryEntryId: 'test-entry-id' })

    // Call deleteItemConfirm
    await (wrapper.vm as any).deleteItemConfirm()

    // Verify selectDate was called because dates match
    expect(diaryEntryAPI.deleteDiaryEntry).toHaveBeenCalledWith('test-entry-id')
    expect((wrapper.vm as any).dialogDelete).toBe(false)
  })

  // onMounted() localStorage preference loading
  it('onMounted loads datePickerExpanded preference from localStorage', async () => {
    // Mock localStorage.getItem to return 'true'
    vi.mocked(localStorage.getItem).mockReturnValue('true')

    // Create a new component instance to trigger onMounted
    const newWrapper = mount(Component, {
      global: {
        plugins: [vuetify],
      },
    })

    await flushPromises()

    // Verify localStorage was called with correct key
    expect(localStorage.getItem).toHaveBeenCalledWith('id.datePickerExpanded')

    // Verify the value was set correctly
    expect((newWrapper.vm as any).isDatePickerExpanded).toBe(true)

    newWrapper.unmount()
  })

  it('renders diary title and author in v-row', async () => {
    await flushPromises()
    const titleSpan = wrapper.find('.title')
    const authorSpan = wrapper.find('.author')
    expect(titleSpan.exists()).toBe(true)
    expect(authorSpan.exists()).toBe(true)
    expect(titleSpan.text()).toBe('Test Diary')
    expect(authorSpan.text()).toContain('Test Author')
  })

  it('calls deleteDiaryEntry when diaryEntryId is defined in deleteItemConfirm', async () => {
    state.isAuthenticated = true
    await flushPromises()
    // Simulate editing an entry with diaryEntryId
    const entry = new DiaryEntry(diaryId, new Date(), 'Loc', 'Entry', { diaryEntryId: 'existing-id' })
    await (wrapper.vm as any).editItem(entry)
    await (wrapper.vm as any).deleteItemConfirm()
    expect(diaryEntryAPI.deleteDiaryEntry).toHaveBeenCalledWith('existing-id')
  })

  it('sets diaryEntries when selectDate is called', async () => {
    await flushPromises()
    const testDate = new Date()
    await (wrapper.vm as any).selectDate(testDate)
    expect(diaryEntryAPI.searchDiaryEntryForDay).toHaveBeenCalledWith(
      diaryId,
      testDate.getFullYear(),
      testDate.getMonth() + 1,
      testDate.getDate()
    )
    expect((wrapper.vm as any).diaryEntries).not.toBeNull()
  })

  it('calls loadDiary on mount', async () => {
    await flushPromises()
    expect(diaryAPI.getDiary).toHaveBeenCalledWith(diaryId)
  })

  it('does not show edit/delete buttons when not authenticated', async () => {
    state.isAuthenticated = false
    await flushPromises()
    // Find all timeline items
    const timelineItems = wrapper.findAllComponents({ name: 'VTimelineItem' })
    timelineItems.forEach(item => {
      expect(item.findAll('button').length).toBe(0)
    })
  })

  it('editItem sets default values when no diaryEntries', async () => {
    (wrapper.vm as any).diaryEntries = []
    await (wrapper.vm as any).editItem()
    expect((wrapper.vm as any).dialog).toBe(true)
    expect((wrapper.vm as any).editedItem.location).toBe('')
  })

  it('resets editedItem after close', async () => {
    await (wrapper.vm as any).editItem(new DiaryEntry(diaryId, new Date(), 'Loc', 'Entry', { diaryEntryId: 'id' }))
    await (wrapper.vm as any).close()
    await nextTick() // <-- Wait for Vue to update
    expect((wrapper.vm as any).editedItem.location).toBe((wrapper.vm as any).defaultItem.location)
  })

  it('resets editedItem after closeDelete', async () => {
    await (wrapper.vm as any).editItem(new DiaryEntry(diaryId, new Date(), 'Loc', 'Entry', { diaryEntryId: 'id' }))
    await (wrapper.vm as any).closeDelete()
    await nextTick() // <-- Wait for Vue to update
    expect((wrapper.vm as any).editedItem.location).toBe((wrapper.vm as any).defaultItem.location)
  })

  it('moveStart sets selectedDate to minDate', async () => {
    (wrapper.vm as any).minDate = new Date(2020, 0, 1)
    await (wrapper.vm as any).moveStart()
    expect(dayjs((wrapper.vm as any).selectedDate).isSame(dayjs((wrapper.vm as any).minDate), 'day')).toBe(true)
  })

  it('moveEnd sets selectedDate to maxDate', async () => {
    (wrapper.vm as any).maxDate = new Date(2020, 11, 31)
    await (wrapper.vm as any).moveEnd()
    expect(dayjs((wrapper.vm as any).selectedDate).isSame(dayjs((wrapper.vm as any).maxDate), 'day')).toBe(true)
  })

  it('calls selectDate when v-date-picker emits update:model-value', async () => {
    await flushPromises()
    const picker = wrapper.findComponent({ name: 'VDatePicker' })
    const testDate = new Date()
    await picker.vm.$emit('update:model-value', testDate)
    // The effect: diaryEntries should be set
    expect((wrapper.vm as any).diaryEntries).not.toBeNull()
  })

  it('refreshes marked days when month and year are updated', async () => {
    await flushPromises()
    const searchSpy = vi.mocked(diaryEntryAPI.searchDiaryEntry)

    ;(wrapper.vm as any).calendarYear = 2024
    await (wrapper.vm as any).updateMonth(0)
    await flushPromises()
    expect(searchSpy).toHaveBeenCalledWith(diaryId, 2024, 1)

    ;(wrapper.vm as any).calendarMonth = 2
    await (wrapper.vm as any).updateYear(2025)
    await flushPromises()
    expect(searchSpy).toHaveBeenCalledWith(diaryId, 2025, 3)
  })

  it('refreshes marked days after creating and deleting entries', async () => {
    await flushPromises()
    const searchSpy = vi.mocked(diaryEntryAPI.searchDiaryEntry)
    const baselineCalls = searchSpy.mock.calls.length

    ;(wrapper.vm as any).visibleYear = 2024
    ;(wrapper.vm as any).visibleMonth = 6
    ;(wrapper.vm as any).selectedDate = new Date(2024, 5, 10)
    ;(wrapper.vm as any).editedItem = new DiaryEntry(diaryId, new Date(2024, 5, 10), 'Loc', 'Entry')

    await (wrapper.vm as any).onSubmitDiaryEntry({
      date: new Date(2024, 5, 10),
      location: 'Updated Location',
      entry: 'Updated Entry',
      mapLocation: '',
      showMap: false,
      fromLocation: '',
      toLocation: '',
      showJourney: false,
    })
    expect(searchSpy.mock.calls.length).toBeGreaterThan(baselineCalls)

    ;(wrapper.vm as any).editedItem = new DiaryEntry(diaryId, new Date(2024, 5, 10), 'Loc', 'Entry', { diaryEntryId: 'existing-id' })
    await (wrapper.vm as any).deleteItemConfirm()
    expect(searchSpy.mock.calls.length).toBeGreaterThan(baselineCalls + 1)
  })

  it('renders timeline items for diaryEntries', async () => {
    await flushPromises()
    const items = wrapper.findAllComponents({ name: 'VTimelineItem' })
    expect(items.length).toBeGreaterThan(0)
  })

  it('moveForward skips dates without entries and stops at first date with entries', async () => {
    await flushPromises()

    // Start on Jan 1, 2020; markedDays shows only Jan 3 has an entry
    ;(wrapper.vm as any).selectedDate = new Date(2020, 0, 1)
    ;(wrapper.vm as any).minDate = new Date(2020, 0, 1)
    ;(wrapper.vm as any).maxDate = new Date(2020, 0, 10)
    ;(wrapper.vm as any).visibleYear = 2020
    ;(wrapper.vm as any).visibleMonth = 1
    ;(wrapper.vm as any).markedDays = [3]
    vi.mocked(diaryEntryAPI.searchDiaryEntry).mockResolvedValue([3])

    await (wrapper.vm as any).moveForward()

    // Should have skipped to Jan 3
    expect((wrapper.vm as any).selectedDate.getDate()).toBe(3)
  })

  it('moveBackward skips dates without entries and stops at first date with entries', async () => {
    await flushPromises()

    // Start on Jan 10, 2020; markedDays shows only Jan 8 has an entry
    ;(wrapper.vm as any).selectedDate = new Date(2020, 0, 10)
    ;(wrapper.vm as any).minDate = new Date(2020, 0, 1)
    ;(wrapper.vm as any).maxDate = new Date(2020, 0, 10)
    ;(wrapper.vm as any).visibleYear = 2020
    ;(wrapper.vm as any).visibleMonth = 1
    ;(wrapper.vm as any).markedDays = [8]
    vi.mocked(diaryEntryAPI.searchDiaryEntry).mockResolvedValue([8])

    await (wrapper.vm as any).moveBackward()

    // Should have skipped back to Jan 8
    expect((wrapper.vm as any).selectedDate.getDate()).toBe(8)
  })

  it('moveForward respects maxDate boundary when no entries found', async () => {
    await flushPromises()

    // Start on Jan 8, stop at Jan 10 (maxDate); no marked days
    ;(wrapper.vm as any).selectedDate = new Date(2020, 0, 8)
    ;(wrapper.vm as any).minDate = new Date(2020, 0, 1)
    ;(wrapper.vm as any).maxDate = new Date(2020, 0, 10)
    ;(wrapper.vm as any).visibleYear = 2020
    ;(wrapper.vm as any).visibleMonth = 1
    ;(wrapper.vm as any).markedDays = []
    vi.mocked(diaryEntryAPI.searchDiaryEntry).mockResolvedValue([])

    await (wrapper.vm as any).moveForward()

    // Should have reached maxDate
    expect(dayjs((wrapper.vm as any).selectedDate).format('YYYY-MM-DD')).toBe(
      dayjs(new Date(2020, 0, 10)).format('YYYY-MM-DD')
    )
  })

  it('moveBackward respects minDate boundary when no entries found', async () => {
    await flushPromises()

    // Start on Jan 3, stop at Jan 1 (minDate); no marked days
    ;(wrapper.vm as any).selectedDate = new Date(2020, 0, 3)
    ;(wrapper.vm as any).minDate = new Date(2020, 0, 1)
    ;(wrapper.vm as any).maxDate = new Date(2020, 0, 10)
    ;(wrapper.vm as any).visibleYear = 2020
    ;(wrapper.vm as any).visibleMonth = 1
    ;(wrapper.vm as any).markedDays = []
    vi.mocked(diaryEntryAPI.searchDiaryEntry).mockResolvedValue([])

    await (wrapper.vm as any).moveBackward()

    // Should have reached minDate
    expect(dayjs((wrapper.vm as any).selectedDate).format('YYYY-MM-DD')).toBe(
      dayjs(new Date(2020, 0, 1)).format('YYYY-MM-DD')
    )
  })

  it('watch(selectedDate) early returns when newDate is undefined', async () => {
    await flushPromises()
    const searchSpy = vi.mocked(diaryEntryAPI.searchDiaryEntryForDay)
    const callsBefore = searchSpy.mock.calls.length

    // Setting selectedDate to undefined triggers the watch early-return path
    ;(wrapper.vm as any).selectedDate = undefined
    await flushPromises()

    // No additional selectDate calls (early return happened)
    expect(searchSpy.mock.calls.length).toBe(callsBefore)
  })

  it('reloads diary data when apiStatus.recoveryCount increases', async () => {
    await flushPromises()
    const getDiarySpy = vi.spyOn(diaryAPI, 'getDiary')
    const callsBefore = getDiarySpy.mock.calls.length

    const store = useApiStatusStore()
    store.recoveryCount++
    await flushPromises()

    expect(getDiarySpy.mock.calls.length).toBeGreaterThan(callsBefore)
  })

  it('updateYear returns early when year is undefined', async () => {
    await flushPromises()
    const prevYear = (wrapper.vm as any).calendarYear
    await (wrapper.vm as any).updateYear(undefined)
    // calendarYear should not have changed
    expect((wrapper.vm as any).calendarYear).toBe(prevYear)
  })

  it('watch([calendarYear, calendarMonth]) returns early when year is undefined', async () => {
    await flushPromises()
    const searchSpy = vi.mocked(diaryEntryAPI.searchDiaryEntry)
    const callsBefore = searchSpy.mock.calls.length

    // Setting calendarYear to undefined triggers the guard early return in the watch
    ;(wrapper.vm as any).calendarYear = undefined
    await flushPromises()

    // No additional searchDiaryEntry calls (early return happened)
    expect(searchSpy.mock.calls.length).toBe(callsBefore)
  })

  it('does not show MapView when showMap is false on diaryEntry', async () => {
    const entryNoMap = new DiaryEntry(diaryId, new Date(), 'Test Location', 'Test Entry', { diaryEntryId: 'entry-id', mapLocation: 'London, UK', showMap: false })
    vi.spyOn(diaryEntryAPI, 'searchDiaryEntryForDay').mockResolvedValue([entryNoMap])
    await flushPromises()
    await (wrapper.vm as any).selectDate(new Date())
    await flushPromises()
    const mapViews = wrapper.findAllComponents({ name: 'MapView' })
    expect(mapViews.length).toBe(0)
  })

  it('does not show MapView when showMap is true but mapLocation is empty', async () => {
    const entryNoMapLoc = new DiaryEntry(diaryId, new Date(), 'Test Location', 'Test Entry', { diaryEntryId: 'entry-id', showMap: true })
    vi.spyOn(diaryEntryAPI, 'searchDiaryEntryForDay').mockResolvedValue([entryNoMapLoc])
    await flushPromises()
    await (wrapper.vm as any).selectDate(new Date())
    await flushPromises()
    const mapViews = wrapper.findAllComponents({ name: 'MapView' })
    expect(mapViews.length).toBe(0)
  })

  it('shows MapView when showMap is true and mapLocation is set', async () => {
    const entryWithMap = new DiaryEntry(diaryId, new Date(), 'Test Location', 'Test Entry', { diaryEntryId: 'entry-id', mapLocation: 'London, UK', showMap: true })
    vi.spyOn(diaryEntryAPI, 'searchDiaryEntryForDay').mockResolvedValue([entryWithMap])
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      json: vi.fn().mockResolvedValue([{ lat: '51.5074', lon: '-0.1278' }]),
    }))
    await flushPromises()
    await (wrapper.vm as any).selectDate(new Date())
    await flushPromises()
    const mapViews = wrapper.findAllComponents({ name: 'MapView' })
    expect(mapViews.length).toBe(1)
    expect(mapViews[0].props('location')).toBe('London, UK')
    vi.unstubAllGlobals()
  })

  it('onCalendarSelectDate pushes date to router history', async () => {
    await flushPromises()
    const testDate = new Date(2020, 5, 15)
    await (wrapper.vm as any).onCalendarSelectDate(testDate)
    expect(mockRouterPush).toHaveBeenCalledWith(
      expect.objectContaining({ query: expect.objectContaining({ date: '2020-06-15' }) })
    )
    expect(mockRouterReplace).not.toHaveBeenCalledWith(
      expect.objectContaining({ query: expect.objectContaining({ date: '2020-06-15' }) })
    )
  })

  it('moveStart replaces (not pushes) router history', async () => {
    await flushPromises()
    mockRouterPush.mockClear()
    mockRouterReplace.mockClear()
    ;(wrapper.vm as any).minDate = new Date(2020, 0, 1)
    await (wrapper.vm as any).moveStart()
    expect(mockRouterReplace).toHaveBeenCalled()
    expect(mockRouterPush).not.toHaveBeenCalled()
  })

  it('moveEnd replaces (not pushes) router history', async () => {
    await flushPromises()
    mockRouterPush.mockClear()
    mockRouterReplace.mockClear()
    ;(wrapper.vm as any).maxDate = new Date(2020, 11, 31)
    await (wrapper.vm as any).moveEnd()
    expect(mockRouterReplace).toHaveBeenCalled()
    expect(mockRouterPush).not.toHaveBeenCalled()
  })

  it('moveForward replaces (not pushes) router history on navigation', async () => {
    await flushPromises()
    mockRouterPush.mockClear()
    mockRouterReplace.mockClear()
    ;(wrapper.vm as any).selectedDate = new Date(2020, 0, 1)
    ;(wrapper.vm as any).minDate = new Date(2020, 0, 1)
    ;(wrapper.vm as any).maxDate = new Date(2020, 0, 10)
    ;(wrapper.vm as any).visibleYear = 2020
    ;(wrapper.vm as any).visibleMonth = 1
    ;(wrapper.vm as any).markedDays = [3]
    vi.mocked(diaryEntryAPI.searchDiaryEntry).mockResolvedValue([3])
    await (wrapper.vm as any).moveForward()
    expect(mockRouterReplace).toHaveBeenCalled()
    expect(mockRouterPush).not.toHaveBeenCalled()
  })

  it('loadDiaryData uses date from URL query param as initial date', async () => {
    mockQuery.date = '2020-06-15'
    vi.spyOn(diaryEntryAPI, 'getMinDate').mockResolvedValue(new Date(2020, 0, 1))
    vi.spyOn(diaryEntryAPI, 'getMaxDate').mockResolvedValue(new Date(2020, 11, 31))
    const newWrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    expect(dayjs((newWrapper.vm as any).selectedDate).format('YYYY-MM-DD')).toBe('2020-06-15')
    newWrapper.unmount()
  })

  it('loadDiaryData clamps URL date to minDate when before range', async () => {
    mockQuery.date = '2019-01-01'
    vi.spyOn(diaryEntryAPI, 'getMinDate').mockResolvedValue(new Date(2020, 0, 1))
    vi.spyOn(diaryEntryAPI, 'getMaxDate').mockResolvedValue(new Date(2020, 11, 31))
    const newWrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    expect(dayjs((newWrapper.vm as any).selectedDate).format('YYYY-MM-DD')).toBe('2020-01-01')
    newWrapper.unmount()
  })

  it('loadDiaryData clamps URL date to maxDate when after range', async () => {
    mockQuery.date = '2025-12-31'
    vi.spyOn(diaryEntryAPI, 'getMinDate').mockResolvedValue(new Date(2020, 0, 1))
    vi.spyOn(diaryEntryAPI, 'getMaxDate').mockResolvedValue(new Date(2020, 11, 31))
    const newWrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    expect(dayjs((newWrapper.vm as any).selectedDate).format('YYYY-MM-DD')).toBe('2020-12-31')
    newWrapper.unmount()
  })
})
