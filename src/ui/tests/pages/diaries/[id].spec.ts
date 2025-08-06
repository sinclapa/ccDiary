/* import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, Mock, MockInstance, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import { useRoute } from 'vue-router'
import { diaryAPI } from '@/services/modules/diaryService'
import { diaryEntryAPI } from '@/services/modules/diaryEntryService'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { state } from '@/services/authentication/msalConfig'
import Component from '@/pages/diaries/[id].vue'
import Diary from '@/services/models/diary'
import DiaryEntry from '@/services/models/diaryEntry'

const vuetify = createVuetify({
  components,
  directives,
})

describe('pages/diaries/[id].vue Implementation Test', () => {
  const diaryId : string = crypto.randomUUID()
  const diary = new Diary('June as a Snowman', 'Mr Puddle', 'Life out of the freezer', diaryId)
  const diaryEntryA = new DiaryEntry(diaryId, new Date(2024, 10, 3, 14, 0, 0), 'Freezer', 'Feeling safe', crypto.randomUUID())
  const diaryEntryB = new DiaryEntry(diaryId, new Date(2024, 10, 3, 14, 0, 0), 'Park', 'Started warm', crypto.randomUUID())
  const minDate = new Date(0, 0, 1)
  const maxDate = new Date(9999, 0, 1)

  // Mock route
  vi.mock('vue-router')
  const mockedUseRoute = useRoute as Mock
  mockedUseRoute.mockReturnValue({ params: { id: diaryId } })

  let wrapper: VueWrapper
  let getDiarySpy: MockInstance
  let getMinDateSpy: MockInstance
  let getMaxDateSpy: MockInstance
  let searchDiaryEntryForDaySpy: MockInstance

  beforeEach(() => {
    state.isAuthenticated = false
    vi.stubEnv('VITE_API', 'http://test')

    // Mock getDiary
    getDiarySpy = vi.spyOn(diaryAPI, 'getDiary').mockReturnValue(
      new Promise(resolve => resolve(diary)
      ))

    getMinDateSpy = vi.spyOn(diaryEntryAPI, 'getMinDate').mockReturnValue(
      new Promise(resolve => resolve(minDate)
      ))

    getMaxDateSpy = vi.spyOn(diaryEntryAPI, 'getMaxDate').mockReturnValue(
      new Promise(resolve => resolve(maxDate)
      ))

    searchDiaryEntryForDaySpy = vi.spyOn(diaryEntryAPI, 'searchDiaryEntryForDay').mockReturnValue(
      new Promise(resolve => resolve([diaryEntryA, diaryEntryB])
      ))

    wrapper = mount(Component, {
      propsData: {},
      global: {
        plugins: [vuetify],
      },
    })
  })

  afterEach(() => {
    state.isAuthenticated = false
    wrapper.unmount()
    getDiarySpy.mockReset()
    getMinDateSpy.mockReset()
    getMaxDateSpy.mockReset()
    searchDiaryEntryForDaySpy.mockReset()
  })

  it('Display controller', async () => {
    expect(getDiarySpy).toHaveBeenCalledOnce()
    expect(getDiarySpy).toBeCalledWith(diaryId)
    expect(getMinDateSpy).toHaveBeenCalledOnce()
    expect(getMinDateSpy).toBeCalledWith(diaryId)
    expect(getMaxDateSpy).toHaveBeenCalledOnce()
    expect(getMaxDateSpy).toBeCalledWith(diaryId)
    expect(searchDiaryEntryForDaySpy).toHaveBeenCalledOnce()
    expect(wrapper.text()).toMatch(diary.title)
    expect(wrapper.text()).toMatch(diary.author)
    expect(wrapper.html()).not.toMatch('Add')
  })

  it('Display controller authenticated', async () => {
    // Arrange
    state.isAuthenticated = true
    await flushPromises()

    // Assert
    expect(getDiarySpy).toHaveBeenCalledOnce()
    expect(getDiarySpy).toBeCalledWith(diaryId)
    expect(wrapper.text()).toMatch(diary.title)
    expect(wrapper.text()).toMatch(diary.author)
    expect(wrapper.html()).toMatch('Add')
  })

  it('shows Add button only when authenticated', async () => {
    state.isAuthenticated = false
    await flushPromises()
    expect(wrapper.html()).not.toMatch('Add')
    state.isAuthenticated = true
    await flushPromises()
    expect(wrapper.html()).toMatch('Add')
  })
})
 */

import { vi } from 'vitest'

// Mock BEFORE importing the component!
vi.mock('vue-router', () => ({
  useRoute: () => ({
    params: { id: 'test-diary-id' }
  })
}))

import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, Mock, MockInstance } from 'vitest'

//import { useRoute } from 'vue-router'
import { diaryAPI } from '@/services/modules/diaryService'
import { diaryEntryAPI } from '@/services/modules/diaryEntryService'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { state } from '@/services/authentication/msalConfig'
import Component from '@/pages/diaries/[id].vue'
import Diary from '@/services/models/diary'
import DiaryEntry from '@/services/models/diaryEntry'
import dayjs from 'dayjs'
import { createVuetify } from 'vuetify'

//const createVuetify = vuetify
const vuetify = createVuetify({ components, directives })


describe('[id].vue', () => {
  const diaryId = 'test-diary-id'
  const diary = new Diary('Test Diary', 'Test Author', 'Test Desc', diaryId)
  const diaryEntry = new DiaryEntry(diaryId, new Date(), 'Test Location', 'Test Entry', 'entry-id')
  const minDate = new Date(2020, 0, 1)
  const maxDate = new Date(2020, 11, 31)

  let wrapper: VueWrapper

  beforeEach(() => {
    vi.resetAllMocks()
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

  it('calls deleteItemConfirm when OK is clicked in delete dialog', async () => {
    state.isAuthenticated = true
    await flushPromises()
    // Open delete dialog
    const deleteBtn = wrapper.findAll('button').find(btn => btn.html().includes('mdi-delete'))
    expect(deleteBtn).toBeTruthy()
    if (deleteBtn) await deleteBtn.trigger('click')
    await flushPromises()
    // Click OK in dialog
    const okBtn = wrapper.findAllComponents({ name: 'VBtn' }).find(btn => btn.text() === 'OK')
    expect(okBtn).toBeTruthy()
    if (okBtn) await okBtn.trigger('click')
    await flushPromises()
    expect((wrapper.vm as any).dialogDelete).toBe(false)
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

  // it('calls selectDate and updateMonth', async () => {
  //   await flushPromises()
  //   // selectDate
  //   await (wrapper.vm as any).selectDate(new Date())
  //   expect(diaryEntryAPI.searchDiaryEntryForDay).toHaveBeenCalled()
  //   // updateMonth
  //   const spy = vi.spyOn(console, 'info').mockImplementation(() => {})
  //   await (wrapper.vm as any).updateMonth('test')
  //   expect(spy).toHaveBeenCalledWith('test')
  //   spy.mockRestore()
  // })

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

  // it('calls updateMonth and logs value', async () => {
  //   const spy = vi.spyOn(console, 'info').mockImplementation(() => {})
  //   await (wrapper.vm as any).updateMonth('month-value')
  //   expect(spy).toHaveBeenCalledWith('month-value')
  //   spy.mockRestore()
  // })

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
// it('calls updateMonth when v-date-picker emits update:month', async () => {
//   await flushPromises()
//   const picker = wrapper.findComponent({ name: 'VDatePicker' })
//   const spy = vi.spyOn(console, 'info').mockImplementation(() => {})
//   await picker.vm.$emit('update:month', 'month')
//   expect(spy).toHaveBeenCalledWith('month')
//   spy.mockRestore()
// })

it('renders timeline items for diaryEntries', async () => {
  await flushPromises()
  const items = wrapper.findAllComponents({ name: 'VTimelineItem' })
  expect(items.length).toBeGreaterThan(0)
})

})


