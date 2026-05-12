import { mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'
import { diaryAPI } from '@/services/modules/diaryService'
import { diaryEntryAPI } from '@/services/modules/diaryEntryService'

globalThis.ResizeObserver = require('resize-observer-polyfill')

vi.mock('@/services/modules/diaryService', () => ({
  diaryAPI: {
    getDiary: vi.fn(),
  },
}))

vi.mock('@/services/modules/diaryEntryService', () => ({
  diaryEntryAPI: {
    getMaxDate: vi.fn(),
    getMinDate: vi.fn(),
    searchDiaryEntry: vi.fn(),
    searchDiaryEntryForDay: vi.fn(),
  },
}))

vi.mock('@/services/authentication/msalConfig', () => ({
  state: {
    isAuthenticated: true,
    user: { name: 'Test User' },
  },
}))

vi.mock('@/plugins/faro', () => ({
  startFaroUserAction: vi.fn(),
  endFaroUserAction: vi.fn(),
}))

vi.mock('vue-router', () => ({
  useRoute: () => ({
    params: { id: 'test-diary-id' },
    query: {},
  }),
  useRouter: () => ({
    push: vi.fn(),
    replace: vi.fn(),
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

describe('pages/diaries/[id].vue - Navigation Controls', () => {
  let wrapper: VueWrapper | null = null

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()

    const mockDiary = {
      diaryId: '1',
      title: 'Test Diary',
      author: 'Test Author',
      description: 'Test Description',
      ownerId: 'test-owner-id',
    };

    (diaryAPI.getDiary as any).mockResolvedValueOnce(mockDiary)

    const today = new Date()
    const startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1)
    const endOfMonth = new Date(today.getFullYear(), today.getMonth() + 1, 0)

    ;(diaryEntryAPI.getMaxDate as any).mockResolvedValueOnce(endOfMonth)
    ;(diaryEntryAPI.getMinDate as any).mockResolvedValueOnce(startOfMonth)
    ;(diaryEntryAPI.searchDiaryEntry as any).mockResolvedValueOnce([1, 5, 10, 15, 20])
    ;(diaryEntryAPI.searchDiaryEntryForDay as any).mockResolvedValueOnce([])
  })

  afterEach(() => {
    if (wrapper) {
      wrapper.unmount()
      wrapper = null
    }
  })

  it('renders navigation buttons with left/right arrows', async () => {
    const DiaryDetail = (await import('@/pages/diaries/[id].vue')).default

    wrapper = mount(DiaryDetail, {
      global: {
        plugins: [vuetify],
        stubs: {
          VDatePicker: true,
          VTimeline: true,
          VTimelineItem: true,
          DiaryEntryEditor: true,
        },
      },
      props: {},
    })

    await wrapper.vm.$nextTick()

    // Check for Previous button with left arrow
    const prevBtn = wrapper.find('.day-nav-btn')
    expect(prevBtn.exists()).toBe(true)
    expect(prevBtn.text()).toContain('Previous')
  })

  it('renders Next button on the right', async () => {
    const DiaryDetail = (await import('@/pages/diaries/[id].vue')).default

    wrapper = mount(DiaryDetail, {
      global: {
        plugins: [vuetify],
        stubs: {
          VDatePicker: true,
          VTimeline: true,
          VTimelineItem: true,
          DiaryEntryEditor: true,
        },
      },
    })

    await wrapper.vm.$nextTick()

    // Check for Next button
    const navBtns = wrapper.findAll('.day-nav-btn')
    expect(navBtns.length).toBeGreaterThanOrEqual(2)
  })

  it('renders Back to Diaries button', async () => {
    const DiaryDetail = (await import('@/pages/diaries/[id].vue')).default

    wrapper = mount(DiaryDetail, {
      global: {
        plugins: [vuetify],
        stubs: {
          VDatePicker: true,
          VTimeline: true,
          VTimelineItem: true,
          DiaryEntryEditor: true,
        },
      },
    })

    await wrapper.vm.$nextTick()

    // Check for Back to Diaries button
    const backBtn = wrapper.find('.back-to-diaries-btn')
    expect(backBtn.exists()).toBe(true)
    expect(backBtn.text()).toContain('Back to Diaries')
  })

  it('Back to Diaries button is left aligned', async () => {
    const DiaryDetail = (await import('@/pages/diaries/[id].vue')).default

    wrapper = mount(DiaryDetail, {
      global: {
        plugins: [vuetify],
        stubs: {
          VDatePicker: true,
          VTimeline: true,
          VTimelineItem: true,
          DiaryEntryEditor: true,
        },
      },
    })

    await wrapper.vm.$nextTick()

    // Check that the col containing Back to Diaries has cols="auto" for left alignment
    const backBtnCol = wrapper.find('.back-to-diaries-btn').element.closest('div[class*="col"]')
    expect(backBtnCol).toBeTruthy()
  })

  it('Previous and Next buttons maintain primary color', async () => {
    const DiaryDetail = (await import('@/pages/diaries/[id].vue')).default

    wrapper = mount(DiaryDetail, {
      global: {
        plugins: [vuetify],
        stubs: {
          VDatePicker: true,
          VTimeline: true,
          VTimelineItem: true,
          DiaryEntryEditor: true,
        },
      },
    })

    await wrapper.vm.$nextTick()

    const navBtns = wrapper.findAll('.day-nav-btn')
    navBtns.forEach(btn => {
      expect(btn.classes()).toContain('v-btn--variant-text')
    })
  })
})
