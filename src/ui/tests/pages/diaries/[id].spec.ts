import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi, Mock, MockInstance } from 'vitest'
import { createVuetify } from 'vuetify'
import { useRoute } from 'vue-router'
import { diaryAPI } from '@/services/modules/diaryService'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import { state } from '@/services/authentication/msalConfig'
import Component from '@/pages/diaries/[id].vue'
import Diary from '@/services/models/diary'

const vuetify = createVuetify({
  components,
  directives,
})

describe('pages/diaries/[id].vue Implementation Test', () => {
  const diaryId : string = crypto.randomUUID()
  const diary = new Diary('June as a Snowman', 'Mr Puddle', 'Life out of the freezer', diaryId)

  // Mock route
  vi.mock('vue-router')
  const mockedUseRoute = useRoute as Mock
  mockedUseRoute.mockReturnValue({ params: { id: diaryId } })

  let wrapper: VueWrapper
  let getDiarySpy: MockInstance

  beforeEach(() => {
    state.isAuthenticated = false
    vi.stubEnv('VITE_API', 'http://test')

    // Mock getDiary
    getDiarySpy = vi.spyOn(diaryAPI, 'getDiary',).mockReturnValue(
      new Promise( resolve => resolve(diary)
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
  })

  it('Display controller', async () => {
    expect(getDiarySpy).toHaveBeenCalledOnce()
    expect(getDiarySpy).toBeCalledWith(diaryId)
    expect(wrapper.text()).toMatch(diary.title)
    expect(wrapper.text()).toMatch(diary.author)
    expect(wrapper.html()).not.toMatch('Add Entry')
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
    expect(wrapper.html()).toMatch('Add Entry')
  })
})
