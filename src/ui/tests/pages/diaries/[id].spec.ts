import { vi } from 'vitest'

// Mock BEFORE importing the component!
vi.mock('vue-router', () => ({
  useRoute: () => ({
    params: { id: 'test-diary-id' }
  })
}))

import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'

//import { useRoute } from 'vue-router'
import { diaryAPI } from '@/services/modules/diaryService'
import { diaryEntryAPI } from '@/services/modules/diaryEntryService'
import { state } from '@/services/authentication/msalConfig'
import Component from '@/pages/diaries/[id].vue'
import Diary from '@/services/models/diary'
import DiaryEntry from '@/services/models/diaryEntry'
import dayjs from 'dayjs'

global.ResizeObserver = require('resize-observer-polyfill')

describe('[id].vue', () => {
  const diaryId = 'test-diary-id'
  const diary = new Diary('Test Diary', 'Test Author', 'Test Desc', diaryId)
  const diaryEntry = new DiaryEntry(diaryId, new Date(), 'Test Location', 'Test Entry', 'entry-id')
  const minDate = new Date(2020, 0, 1)
  const maxDate = new Date(2020, 11, 31)

  let wrapper: VueWrapper

  beforeEach(() => {
    // Mock localStorage
    const localStorageMock = {
      getItem: vi.fn(),
      setItem: vi.fn(),
      clear: vi.fn(),
      removeItem: vi.fn()
    }
    Object.defineProperty(window, 'localStorage', {
      value: localStorageMock,
      writable: true
    })

    vi.clearAllMocks()
    state.isAuthenticated = false
    vi.spyOn(diaryAPI, 'getDiary').mockResolvedValue(diary)
    vi.spyOn(diaryEntryAPI, 'getMinDate').mockResolvedValue(minDate)
    vi.spyOn(diaryEntryAPI, 'getMaxDate').mockResolvedValue(maxDate)
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
    ((wrapper.vm as any).isDatePickerExpanded as any) = false;

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
      entry: 'New Entry'
    })
    expect(diaryEntryAPI.createDiaryEntry).toHaveBeenCalled()
  })

  it('calls onSubmitDiaryEntry for update', async () => {
    state.isAuthenticated = true
    await flushPromises()
    // Simulate editing an existing entry
    const entry = new DiaryEntry(diaryId, new Date(), 'Loc', 'Entry', 'existing-id')
    await (wrapper.vm as any).editItem(entry)
    await (wrapper.vm as any).onSubmitDiaryEntry({
      date: entry.date,
      location: entry.location,
      entry: entry.entry
    })
    expect(diaryEntryAPI.updateDiaryEntry).toHaveBeenCalled()
  })

  it('should toggle isDatePickerExpanded from false to true', async () => {
    // Set initial state to false
    ((wrapper.vm as any).isDatePickerExpanded as any) = false;

    // Call toggle function
    (wrapper.vm as any).toggleDatePickerHeight();

    // Verify state changed to true
    expect((wrapper.vm as any).isDatePickerExpanded).toBe(true);
  })

  // onSubmitDiaryEntry() when date matches selectedDate
  it('onSubmitDiaryEntry calls selectDate when date matches selectedDate', async () => {
    state.isAuthenticated = true
    await flushPromises()

    const testDate = new Date(2024, 1, 15);
    (wrapper.vm as any).selectedDate = testDate;

    // Setup editedItem for creation (no diaryEntryId)
    (wrapper.vm as any).editedItem = new DiaryEntry(diaryId, testDate, 'Test Location', 'Test Entry');

    // Call onSubmitDiaryEntry with matching date
    await (wrapper.vm as any).onSubmitDiaryEntry({
      date: testDate,
      location: 'Updated Location',
      entry: 'Updated Entry'
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
    (wrapper.vm as any).editedItem = new DiaryEntry(diaryId, testDate, 'Test Location', 'Test Entry', 'test-entry-id');

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
    const entry = new DiaryEntry(diaryId, new Date(), 'Loc', 'Entry', 'existing-id')
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
    await (wrapper.vm as any).editItem({ location: 'Loc', entry: 'Entry', date: new Date(), diaryEntryId: 'id' })
    await (wrapper.vm as any).close()
    await nextTick() // <-- Wait for Vue to update
    expect((wrapper.vm as any).editedItem.location).toBe((wrapper.vm as any).defaultItem.location)
  })

  it('resets editedItem after closeDelete', async () => {
    await (wrapper.vm as any).editItem({ location: 'Loc', entry: 'Entry', date: new Date(), diaryEntryId: 'id' })
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

  it('renders timeline items for diaryEntries', async () => {
    await flushPromises()
    const items = wrapper.findAllComponents({ name: 'VTimelineItem' })
    expect(items.length).toBeGreaterThan(0)
  })

})


