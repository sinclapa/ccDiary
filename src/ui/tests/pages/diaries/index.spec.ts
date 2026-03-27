import { mount } from '@vue/test-utils'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'
import { state } from '@/services/authentication/msalConfig'
import Index from '@/pages/diaries/index.vue'
import { diaryAPI } from '@/services/modules/diaryService'

global.ResizeObserver = require('resize-observer-polyfill')

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
    expect(wrapper.findComponent({ name: 'VDataTable'}).exists()).toBe(true)
  })

  it('should fetch diaries on mount', async () => {
    const mockDiaries = [
      { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1' },
    ];
    (diaryAPI.getDiaries as any).mockResolvedValueOnce(mockDiaries)
    await wrapper.vm.data()
    expect(diaryAPI.getDiaries).toHaveBeenCalled()
    expect(wrapper.vm.diaries).toEqual(mockDiaries)
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
      { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1' },
    ];
    (diaryAPI.getDiaries as any).mockResolvedValueOnce(mockDiaries)
    await wrapper.vm.data()
    expect(wrapper.html()).toContain('_delete')
    state.isAuthenticated = false
    wrapper = mount(Index)
    expect(wrapper.html()).not.toContain('_delete')
  })

  it('sets defaultItem.author to empty string if state.user.name is null', async () => {
    // Arrange: set user.name to null
    if (state.user) {
      state.user.name = undefined
    }
    // Remount to trigger onMounted
    const wrapper = mount(Index, {
      global: { plugins: [vuetify] }
    })
    // Wait for onMounted to finish
    await new Promise(resolve => setTimeout(resolve, 0))
    expect((wrapper.vm as any).defaultItem.author).toBe('')
  })

  it('renders the root div and table with data', async () => {
    // Provide at least one diary so the table renders a row
    const mockDiaries = [
      { diaryId: '1', title: 'Diary 1', author: 'Author 1', description: 'Description 1' },
    ];
    (diaryAPI.getDiaries as any).mockResolvedValueOnce(mockDiaries);

    const wrapper = mount(Index, {
      global: { plugins: [vuetify] }
    });

    // Wait for onMounted and data fetching
    await new Promise(resolve => setTimeout(resolve, 0));
    await wrapper.vm.$nextTick();

    // Check root div exists
    expect(wrapper.find('div').exists()).toBe(true);
    // Check table row is rendered
    expect(wrapper.html()).toContain('Diary 1');
  });
})
