import { beforeEach, describe, expect, it, vi } from 'vitest'

import { endFaroUserAction, FARO_CONSENT_KEY, initFaro, pushFaroEvent, startFaroUserAction } from '../faro'
import { getAppConfigField } from '@/utils/appConfig'

const mockPushEvent = vi.fn()
const mockStartUserAction = vi.fn()
const mockEndUserAction = vi.fn()

const { mockInitializeFaro, mockGetWebInstrumentations, MockTracingInstrumentation, mockGetDefaultOTELInstrumentations } = vi.hoisted(() => ({
  mockInitializeFaro: vi.fn(),
  mockGetWebInstrumentations: vi.fn(() => []),
  MockTracingInstrumentation: vi.fn(),
  mockGetDefaultOTELInstrumentations: vi.fn(opts => [{ name: 'mock-otel', options: opts }]),
}))

vi.mock('@grafana/faro-web-sdk', () => ({
  initializeFaro: mockInitializeFaro,
  getWebInstrumentations: mockGetWebInstrumentations,
  TransportItemType: {
    LOG: 'log',
    EXCEPTION: 'exception',
    MEASUREMENT: 'measurement',
    TRACE: 'trace',
    EVENT: 'event',
  },
}))

vi.mock('@grafana/faro-web-tracing', () => ({
  TracingInstrumentation: MockTracingInstrumentation,
  getDefaultOTELInstrumentations: mockGetDefaultOTELInstrumentations,
}))

vi.mock('@/utils/appConfig', () => ({
  getAppConfigField: vi.fn(),
}))

function makeSpan (urlFull?: string, httpUrl?: string) {
  const attributes: Record<string, unknown> = {}
  if (urlFull !== undefined) attributes['url.full'] = urlFull
  if (httpUrl !== undefined) attributes['http.url'] = httpUrl
  return { attributes, updateName: vi.fn() }
}

describe('initFaro', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.removeItem(FARO_CONSENT_KEY)
  })

  it('does not initialize when VITE_FARO_URL is NOT_SET', () => {
    vi.mocked(getAppConfigField).mockReturnValue('NOT_SET')
    initFaro()
    expect(mockInitializeFaro).not.toHaveBeenCalled()
  })

  it('does not initialize when VITE_FARO_URL is empty', () => {
    vi.mocked(getAppConfigField).mockReturnValue('')
    initFaro()
    expect(mockInitializeFaro).not.toHaveBeenCalled()
  })

  describe('when VITE_FARO_URL is set', () => {
    const faroUrl = 'https://faro-collector.example.com/collect/abc123'

    function setupMocks (overrides: Record<string, string> = {}) {
      localStorage.setItem(FARO_CONSENT_KEY, 'true')
      vi.mocked(getAppConfigField).mockImplementation((field, opts) => {
        const values: Record<string, string> = {
          VITE_FARO_URL: faroUrl,
          VITE_API: 'NOT_SET',
          VITE_ENVIRONMENT: 'staging',
          ...overrides,
        }
        return values[field] ?? opts?.defaultValue ?? 'NOT_SET'
      })
    }

    it('calls initializeFaro with correct app config', () => {
      setupMocks()
      initFaro()
      expect(mockInitializeFaro).toHaveBeenCalledOnce()
      const config = mockInitializeFaro.mock.calls[0][0]
      expect(config.url).toBe(faroUrl)
      expect(config.app.name).toBe('ccdiary-ui')
      expect(config.app.version).toBe(__APP_VERSION__)
      expect(config.app.environment).toBe('staging')
    })

    it('sets propagateTraceHeaderCorsUrls from VITE_API', () => {
      setupMocks({ VITE_API: 'https://api.example.com' })
      initFaro()
      const otelOptions = mockGetDefaultOTELInstrumentations.mock.calls[0][0]
      expect(otelOptions.propagateTraceHeaderCorsUrls).toHaveLength(1)
      expect('https://api.example.com/v1/diary').toMatch(otelOptions.propagateTraceHeaderCorsUrls[0])
      expect('https://other.example.com/v1/diary').not.toMatch(otelOptions.propagateTraceHeaderCorsUrls[0])
    })

    it('uses empty propagateTraceHeaderCorsUrls when VITE_API is not set', () => {
      setupMocks({ VITE_API: 'NOT_SET' })
      initFaro()
      const otelOptions = mockGetDefaultOTELInstrumentations.mock.calls[0][0]
      expect(otelOptions.propagateTraceHeaderCorsUrls).toHaveLength(0)
    })

    it('ignores fetch spans to the Faro collector URL', () => {
      setupMocks()
      initFaro()
      const otelOptions = mockGetDefaultOTELInstrumentations.mock.calls[0][0]
      const collectorPattern = otelOptions.ignoreUrls.find((p: unknown) => p instanceof RegExp && p.test(faroUrl)) as RegExp
      expect(collectorPattern).toBeDefined()
      expect(collectorPattern.test('https://other.example.com/collect')).toBe(false)
    })

    it('includes ignoreErrors pattern matching dynamic import failures', () => {
      setupMocks()
      initFaro()
      const { ignoreErrors } = mockInitializeFaro.mock.calls[0][0]
      expect(ignoreErrors).toBeDefined()
      const pattern = ignoreErrors.find((p: unknown) => p instanceof RegExp) as RegExp
      expect(pattern).toBeDefined()
      expect(pattern.test('Failed to fetch dynamically imported module: http://localhost:8080/src/pages/diaries/[id].vue')).toBe(true)
      expect(pattern.test('Some unrelated error')).toBe(false)
    })

    describe('beforeSend', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      let beforeSend: (item: any) => any

      beforeEach(() => {
        setupMocks()
        initFaro()
        beforeSend = mockInitializeFaro.mock.calls[0][0].beforeSend
      })

      it.each([
        ['dynamic import error', 'console.error: Dynamic import error Failed to fetch dynamically imported module: http://localhost:8080/src/pages/diaries/[id].vue'],
        ['Faro collector URL', `console.error: Failed to send to ${faroUrl}`],
      ])('filters log events containing %s', (_, message) => {
        const logItem = { type: 'log', payload: { message }, meta: {} }
        expect(beforeSend(logItem)).toBeNull()
      })

      it('passes through unrelated log events', () => {
        const logItem = { type: 'log', payload: { message: 'console.error: Some unrelated application error' }, meta: {} }
        expect(beforeSend(logItem)).toBe(logItem)
      })

      it('passes through non-log event types unmodified', () => {
        const exceptionItem = {
          type: 'exception',
          payload: { value: 'Failed to fetch dynamically imported module: http://localhost:8080/foo.vue', type: 'TypeError' },
          meta: {},
        }
        expect(beforeSend(exceptionItem)).toBe(exceptionItem)
      })
    })

    it('enables persistent session tracking', () => {
      setupMocks()
      initFaro()
      const config = mockInitializeFaro.mock.calls[0][0]
      expect(config.sessionTracking).toMatchObject({ enabled: true, persistent: true })
    })

    describe('applyCustomAttributesOnSpan', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      let cb: (span: any, init: RequestInit, response?: unknown) => void
      const GUID = '550e8400-e29b-41d4-a716-446655440000'

      beforeEach(() => {
        setupMocks({ VITE_API: 'https://api.example.com' })
        initFaro()
        const otelOptions = mockGetDefaultOTELInstrumentations.mock.calls[0][0]
        cb = otelOptions.fetchInstrumentationOptions?.applyCustomAttributesOnSpan
      })

      it.each<[string, string | undefined, string | undefined, string | undefined, string]>([
        ['renames span using url.full (stable semconv)', 'https://api.example.com/v1/Diary/Get', undefined, 'GET', 'GET /v1/Diary/Get'],
        ['falls back to http.url when url.full is absent', undefined, 'https://api.example.com/v1/Diary/Create', 'POST', 'POST /v1/Diary/Create'],
        ['normalizes GUID in Diary path to {id}', `https://api.example.com/v1/Diary/Get/${GUID}`, undefined, 'GET', 'GET /v1/Diary/Get/{id}'],
        ['normalizes GUID in Diary Delete path to {id}', `https://api.example.com/v1/Diary/Delete/${GUID}`, undefined, 'DELETE', 'DELETE /v1/Diary/Delete/{id}'],
        ['normalizes GUID in DiaryEntry path to {id}', `https://api.example.com/v1/DiaryEntry/GetMinDate/${GUID}`, undefined, 'GET', 'GET /v1/DiaryEntry/GetMinDate/{id}'],
        ['preserves year/month/day numeric segments in Search path', `https://api.example.com/v1/DiaryEntry/Search/${GUID}/2024/3/15`, undefined, 'GET', 'GET /v1/DiaryEntry/Search/{id}/2024/3/15'],
        ['defaults method to GET when RequestInit has no method', 'https://api.example.com/v1/Diary/Get', undefined, undefined, 'GET /v1/Diary/Get'],
        ['uppercases the HTTP method', 'https://api.example.com/v1/DiaryEntry/Update', undefined, 'put', 'PUT /v1/DiaryEntry/Update'],
      ])('%s', (_, urlFull, httpUrl, method, expected) => {
        const span = makeSpan(urlFull, httpUrl)
        cb(span, method ? { method } : {}, {})
        expect(span.updateName).toHaveBeenCalledWith(expected)
      })

      it('does not rename span when attributes object is absent', () => {
        const span = { updateName: vi.fn() }
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        cb(span as any, { method: 'GET' }, {})
        expect(span.updateName).not.toHaveBeenCalled()
      })

      it.each([
        ['url attributes are empty string', '', ''],
        ['url is malformed', 'not-a-valid-url', undefined],
      ])('does not rename span when %s', (_, urlFull, httpUrl) => {
        const span = makeSpan(urlFull, httpUrl)
        cb(span, { method: 'GET' }, {})
        expect(span.updateName).not.toHaveBeenCalled()
      })
    })
  })
})

describe('pushFaroEvent', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.removeItem(FARO_CONSENT_KEY)
  })

  it('calls faro.api.pushEvent with the given name and attributes', () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')
    vi.mocked(getAppConfigField).mockReturnValue('https://faro.example.com')
    mockInitializeFaro.mockReturnValue({ api: { pushEvent: mockPushEvent } })
    initFaro()
    pushFaroEvent('diary.navigation.forward', { diaryId: 'abc' })
    expect(mockPushEvent).toHaveBeenCalledWith('diary.navigation.forward', { diaryId: 'abc' })
  })

  it('calls faro.api.pushEvent with no attributes when omitted', () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')
    vi.mocked(getAppConfigField).mockReturnValue('https://faro.example.com')
    mockInitializeFaro.mockReturnValue({ api: { pushEvent: mockPushEvent } })
    initFaro()
    pushFaroEvent('diary.navigation.start')
    expect(mockPushEvent).toHaveBeenCalledWith('diary.navigation.start', undefined)
  })

  it('does nothing when faro is not initialized', () => {
    vi.mocked(getAppConfigField).mockReturnValue('NOT_SET')
    initFaro()
    expect(() => pushFaroEvent('diary.navigation.forward')).not.toThrow()
    expect(mockPushEvent).not.toHaveBeenCalled()
  })
})

describe('startFaroUserAction', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.removeItem(FARO_CONSENT_KEY)
  })

  it('calls faro.api.startUserAction with name and attributes', () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')
    vi.mocked(getAppConfigField).mockReturnValue('https://faro.example.com')
    mockInitializeFaro.mockReturnValue({ api: { startUserAction: mockStartUserAction } })
    initFaro()
    startFaroUserAction('diary-navigation-forward', { diaryId: 'abc' })
    expect(mockStartUserAction).toHaveBeenCalledWith('diary-navigation-forward', { diaryId: 'abc' })
  })

  it('calls faro.api.startUserAction with empty attributes when omitted', () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')
    vi.mocked(getAppConfigField).mockReturnValue('https://faro.example.com')
    mockInitializeFaro.mockReturnValue({ api: { startUserAction: mockStartUserAction } })
    initFaro()
    startFaroUserAction('diary-navigation-start')
    expect(mockStartUserAction).toHaveBeenCalledWith('diary-navigation-start', {})
  })

  it('does nothing when faro is not initialized', () => {
    vi.mocked(getAppConfigField).mockReturnValue('NOT_SET')
    initFaro()
    expect(() => startFaroUserAction('diary-navigation-forward')).not.toThrow()
    expect(mockStartUserAction).not.toHaveBeenCalled()
  })
})

describe('endFaroUserAction', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.removeItem(FARO_CONSENT_KEY)
  })

  it('calls end() on the action returned by startUserAction', () => {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')
    vi.mocked(getAppConfigField).mockReturnValue('https://faro.example.com')
    mockStartUserAction.mockReturnValue({ end: mockEndUserAction })
    mockInitializeFaro.mockReturnValue({ api: { startUserAction: mockStartUserAction } })
    initFaro()
    startFaroUserAction('diary-navigation-forward')
    endFaroUserAction()
    expect(mockEndUserAction).toHaveBeenCalledOnce()
  })

  it('does nothing when no action was started', () => {
    vi.mocked(getAppConfigField).mockReturnValue('NOT_SET')
    initFaro()
    expect(() => endFaroUserAction()).not.toThrow()
    expect(mockEndUserAction).not.toHaveBeenCalled()
  })
})
