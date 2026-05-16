import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'
import { state } from '@/services/authentication/msalConfig'
import { useApiStatusStore } from '@/stores/apiStatus'
import { useAuthStore } from '@/stores/auth'
import Index from '@/pages/diaries/index.vue'
import { diaryAPI } from '@/services/modules/diaryService'

globalThis.ResizeObserver = require('resize-observer-polyfill')

vi.mock('@/services/modules/diaryService', () => ({
  diaryAPI: {
    getDiaries: vi.fn(),
    createDiary: vi.fn(),
    updateDiary: vi.fn(),
    deleteDiary: vi.fn(),
  },
}))

vi.mock('@/services/authentication/msalConfig', () => ({
  state: {
    isAuthenticated: true,
    user: { name: 'Test User' },
  },
}))

describe('pages/diaries/index.vue', () => {
  let wrapper: any
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    wrapper = mount(Index, {
      global: {
        plugins: [vuetify],
      },
    })
  })

  it('should render the component', () => {
    expect(wrapper.exists()).toBe(true)
    expect(wrapper.findComponent({ name: 'VContainer' }).exists()).toBe(true)
  })

  it('should fetch diaries on mount', async () => {
    const mockDiaries = [
      { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1' },
    ];
    (diaryAPI.getDiaries as any).mockResolvedValueOnce({ items: mockDiaries, totalCount: 1, page: 1, pageSize: 12 })
    await wrapper.vm.data()
    expect(diaryAPI.getDiaries).toHaveBeenCalled()
    expect(wrapper.vm.diaries).toEqual(mockDiaries)
    expect(wrapper.vm.totalCount).toBe(1)
  })

  it('should open the dialog for adding a diary', async () => {
    await wrapper.vm.editItem()
    expect(wrapper.vm.dialog).toBe(true)
    expect(wrapper.vm.editedItem.diaryId).toBeUndefined()
  })

  it('should open the dialog for editing a diary', async () => {
    const diary = { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1' }
    await wrapper.vm.editItem(diary)
    expect(wrapper.vm.dialog).toBe(true)
    expect(wrapper.vm.editedItem).toEqual(diary)
  })

  it('should close the dialog', async () => {
    wrapper.vm.close()
    await wrapper.vm.data()
    expect(wrapper.vm.dialog).toBe(false)
    expect(wrapper.vm.editedItem).toEqual(wrapper.vm.defaultItem)
  })

  it('should open the delete confirmation dialog', async () => {
    const diary = { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1' }
    await wrapper.vm.deleteItem(diary)
    expect(wrapper.vm.dialogDelete).toBe(true)
    expect(wrapper.vm.editedItem).toEqual(diary)
  })

  it('should close the delete confirmation dialog', async () => {
    wrapper.vm.closeDelete()
    await wrapper.vm.data()
    expect(wrapper.vm.dialogDelete).toBe(false)
    expect(wrapper.vm.editedItem).toEqual(wrapper.vm.defaultItem)
  })

  it('should confirm and delete a diary', async () => {
    const diaryId = '1'
    wrapper.vm.editedItem.diaryId = diaryId
    await wrapper.vm.deleteItemConfirm()
    expect(diaryAPI.deleteDiary).toHaveBeenCalledWith(diaryId)
    expect(diaryAPI.getDiaries).toHaveBeenCalled()
    expect(wrapper.vm.dialogDelete).toBe(false)
  })

  it('should add a new diary', async () => {
    const payload = { title: 'New Diary', author: 'Test User2', description: 'New Description' }
    wrapper.vm.editedItem.diaryId = undefined
    await wrapper.vm.onAddDiary(payload)
    expect(diaryAPI.createDiary).toHaveBeenCalledWith({
      diaryId: undefined,
      title: 'New Diary',
      author: 'Test User2',
      description: 'New Description',
    })
    expect(diaryAPI.getDiaries).toHaveBeenCalled()
    expect(wrapper.vm.dialog).toBe(false)
  })

  it('should update an existing diary', async () => {
    const payload = { title: 'Updated Diary', author: 'Test User', description: 'Updated Description' }
    wrapper.vm.editedItem.diaryId = '1'
    await wrapper.vm.onAddDiary(payload)
    expect(diaryAPI.updateDiary).toHaveBeenCalledWith({
      diaryId: '1',
      title: 'Updated Diary',
      author: 'Test User',
      description: 'Updated Description',
    })
    expect(diaryAPI.getDiaries).toHaveBeenCalled()
    expect(wrapper.vm.dialog).toBe(false)
  })

  it('should conditionally render buttons based on authentication', async () => {
    const mockDiaries = [
      { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1', ownerId: 'oid-1' },
    ];
    (diaryAPI.getDiaries as any).mockResolvedValueOnce({ items: mockDiaries, totalCount: 1, page: 1, pageSize: 12 })

    // Set user as admin so canEdit returns true
    const authStore = useAuthStore()
    authStore.appUser = { userId: 'u1', displayName: 'Admin', email: 'a@b.com', role: 'diary-admin', entraObjectId: 'oid-1' }

    await wrapper.vm.data()
    expect(wrapper.html()).toContain('_delete')

    // Clear user — no longer admin/contributor
    authStore.appUser = null
    await wrapper.vm.$nextTick()
    expect(wrapper.html()).not.toContain('_delete')
  })

  it('sets defaultItem.author to empty string if state.user.name is null', async () => {
    // Arrange: set user.name to null
    if (state.user) {
      state.user.name = undefined
    }
    // Remount to trigger onMounted
    const wrapper = mount(Index, {
      global: { plugins: [vuetify] },
    })
    // Wait for onMounted to finish
    await new Promise(resolve => setTimeout(resolve, 0))
    expect((wrapper.vm as any).defaultItem.author).toBe('')
  })

  it('renders the root div and cards with data', async () => {
    // Provide at least one diary so a card renders
    const mockDiaries = [
      { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1' },
    ];
    (diaryAPI.getDiaries as any).mockResolvedValueOnce({ items: mockDiaries, totalCount: 1, page: 1, pageSize: 12 })

    const wrapper = mount(Index, {
      global: { plugins: [vuetify] },
    })

    // Wait for onMounted and data fetching
    await new Promise(resolve => setTimeout(resolve, 0))
    await wrapper.vm.$nextTick()

    // Check root div exists
    expect(wrapper.find('div').exists()).toBe(true)
    // Check table row is rendered
    expect(wrapper.html()).toContain('Diary 1')
  })

  it('search button renders in header row', () => {
    const searchBtn = wrapper.findAll('button').find((btn: any) =>
      btn.attributes('aria-label') === 'Search diaries'
    )
    expect(searchBtn).toBeTruthy()
  })

  it('clicking search button expands the search text field', async () => {
    // Search field hidden before toggle
    const inputsBefore = wrapper.findAll('input').filter((i: any) =>
      i.attributes('placeholder')?.toLowerCase().includes('search')
    )
    expect(inputsBefore.length).toBe(0)

    const searchBtn = wrapper.findAll('button').find((btn: any) =>
      btn.attributes('aria-label') === 'Search diaries'
    )
    await searchBtn?.trigger('click')
    await wrapper.vm.$nextTick()

    const inputsAfter = wrapper.findAll('input').filter((i: any) =>
      i.attributes('placeholder')?.toLowerCase().includes('search')
    )
    expect(inputsAfter.length).toBeGreaterThan(0)
  })

  it('clicking search button again collapses the search field and clears search', async () => {
    // Open
    await wrapper.vm.toggleSearch()
    wrapper.vm.searchTerm = 'Wartime'
    await wrapper.vm.$nextTick()
    expect(wrapper.vm.searchExpanded).toBe(true)

    // Close
    await wrapper.vm.toggleSearch()
    expect(wrapper.vm.searchExpanded).toBe(false)
    expect(wrapper.vm.searchTerm).toBe('')
  })

  it('typing in search calls getDiaries with search param', async () => {
    vi.useFakeTimers()
    ;(diaryAPI.getDiaries as any).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 12 })

    wrapper.vm.searchTerm = 'Wartime'
    await wrapper.vm.$nextTick()  // let Vue flush the watcher
    vi.advanceTimersByTime(350)   // fire the debounce setTimeout

    const calls = (diaryAPI.getDiaries as any).mock.calls
    const searchCall = calls.find((c: any[]) => c[2] === 'Wartime')
    expect(searchCall).toBeTruthy()
    vi.useRealTimers()
  })

  it('clearing search resets page and calls getDiaries without search param', async () => {
    vi.useFakeTimers()
    ;(diaryAPI.getDiaries as any).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 12 })

    // Set a search term and fire the debounce
    wrapper.vm.searchTerm = 'Wartime'
    wrapper.vm.currentPage = 3
    await wrapper.vm.$nextTick()
    vi.advanceTimersByTime(350)

    // Clear the search and fire the debounce again
    wrapper.vm.searchTerm = ''
    await wrapper.vm.$nextTick()
    vi.advanceTimersByTime(350)
    await wrapper.vm.$nextTick()

    // Page should be reset to 1
    expect(wrapper.vm.currentPage).toBe(1)

    // The last getDiaries call should have no search param (undefined)
    const calls = (diaryAPI.getDiaries as any).mock.calls
    const lastCall = calls[calls.length - 1]
    expect(lastCall[2]).toBeUndefined()
    vi.useRealTimers()
  })

  it('reloads diaries when apiStatus.recoveryCount increases', async () => {
    const store = useApiStatusStore()
    const getDiariesSpy = diaryAPI.getDiaries as ReturnType<typeof vi.fn>
    const callsBefore = getDiariesSpy.mock.calls.length

    store.recoveryCount++
    await new Promise(resolve => setTimeout(resolve, 0))

    expect(getDiariesSpy.mock.calls.length).toBeGreaterThan(callsBefore)
  })
})
