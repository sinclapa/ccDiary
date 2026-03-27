import { beforeEach, describe, expect, it, vi } from 'vitest'
import { getSystemTheme } from '../browserTheme'

const createMediaQueryList = (query: string, matches: boolean): MediaQueryList => ({
  matches,
  media: query,
  onchange: null,
  addListener: vi.fn<MediaQueryList['addListener']>(),
  removeListener: vi.fn<MediaQueryList['removeListener']>(),
  addEventListener: vi.fn<MediaQueryList['addEventListener']>(),
  removeEventListener: vi.fn<MediaQueryList['removeEventListener']>(),
  dispatchEvent: vi.fn<MediaQueryList['dispatchEvent']>(() => true),
})

describe('getSystemTheme', () => {
  beforeEach(() => {
    // Reset window.matchMedia mock before each test
    vi.resetAllMocks()
  })

  it('returns dark when prefers-color-scheme is dark', () => {
    vi.spyOn(globalThis, 'matchMedia').mockImplementation((query) =>
      createMediaQueryList(query, query === '(prefers-color-scheme: dark)'),
    )

    const theme = getSystemTheme()
    expect(theme).toBe('dark')
  })

  it('returns light when prefers-color-scheme is light', () => {
    vi.spyOn(globalThis, 'matchMedia').mockImplementation((query) =>
      createMediaQueryList(query, false),
    )

    const theme = getSystemTheme()
    expect(theme).toBe('light')
  })

  it('returns light when matchMedia is not available', () => {
    vi.spyOn(globalThis, 'matchMedia').mockImplementation((_query: string) => {
      throw new Error('matchMedia not supported')
    })

    const theme = getSystemTheme()
    expect(theme).toBe('light')
  })
})