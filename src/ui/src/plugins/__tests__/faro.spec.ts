import { beforeEach, describe, expect, it, vi } from 'vitest'

import { initFaro } from '../faro'
import { getAppConfigField } from '@/utils/appConfig'

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

      it('filters log events containing the dynamic import error message', () => {
        const logItem = {
          type: 'log',
          payload: { message: 'console.error: Dynamic import error Failed to fetch dynamically imported module: http://localhost:8080/src/pages/diaries/[id].vue' },
          meta: {},
        }
        expect(beforeSend(logItem)).toBeNull()
      })

      it('filters log events containing the Faro collector URL', () => {
        const logItem = {
          type: 'log',
          payload: { message: `console.error: Failed to send to ${faroUrl}` },
          meta: {},
        }
        expect(beforeSend(logItem)).toBeNull()
      })

      it('passes through unrelated log events', () => {
        const logItem = {
          type: 'log',
          payload: { message: 'console.error: Some unrelated application error' },
          meta: {},
        }
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

    describe('applyCustomAttributesOnSpan', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      let cb: (span: any, init: RequestInit, response: Response) => void

      beforeEach(() => {
        setupMocks({ VITE_API: 'https://api.example.com' })
        initFaro()
        const otelOptions = mockGetDefaultOTELInstrumentations.mock.calls[0][0]
        cb = otelOptions.fetchInstrumentationOptions?.applyCustomAttributesOnSpan
      })

      it('renames span using url.full (stable semconv)', () => {
        const span = makeSpan('https://api.example.com/v1/Diary/Get')
        cb(span, { method: 'GET' }, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('GET /v1/Diary/Get')
      })

      it('falls back to http.url when url.full is absent', () => {
        const span = makeSpan(undefined, 'https://api.example.com/v1/Diary/Create')
        cb(span, { method: 'POST' }, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('POST /v1/Diary/Create')
      })

      it('normalizes GUID in Diary path to {id}', () => {
        const span = makeSpan('https://api.example.com/v1/Diary/Get/550e8400-e29b-41d4-a716-446655440000')
        cb(span, { method: 'GET' }, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('GET /v1/Diary/Get/{id}')
      })

      it('normalizes GUID in Diary Delete path to {id}', () => {
        const span = makeSpan('https://api.example.com/v1/Diary/Delete/550e8400-e29b-41d4-a716-446655440000')
        cb(span, { method: 'DELETE' }, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('DELETE /v1/Diary/Delete/{id}')
      })

      it('normalizes GUID in DiaryEntry path to {id}', () => {
        const span = makeSpan('https://api.example.com/v1/DiaryEntry/GetMinDate/550e8400-e29b-41d4-a716-446655440000')
        cb(span, { method: 'GET' }, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('GET /v1/DiaryEntry/GetMinDate/{id}')
      })

      it('preserves year/month/day numeric segments in Search path', () => {
        const span = makeSpan('https://api.example.com/v1/DiaryEntry/Search/550e8400-e29b-41d4-a716-446655440000/2024/3/15')
        cb(span, { method: 'GET' }, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('GET /v1/DiaryEntry/Search/{id}/2024/3/15')
      })

      it('defaults method to GET when RequestInit has no method', () => {
        const span = makeSpan('https://api.example.com/v1/Diary/Get')
        cb(span, {}, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('GET /v1/Diary/Get')
      })

      it('uppercases the HTTP method', () => {
        const span = makeSpan('https://api.example.com/v1/DiaryEntry/Update')
        cb(span, { method: 'put' }, {} as Response)
        expect(span.updateName).toHaveBeenCalledWith('PUT /v1/DiaryEntry/Update')
      })

      it('does not rename span when attributes object is absent', () => {
        const span = { updateName: vi.fn() }
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        cb(span as any, { method: 'GET' }, {} as Response)
        expect(span.updateName).not.toHaveBeenCalled()
      })

      it('does not rename span when url attributes are empty string', () => {
        const span = makeSpan('', '')
        cb(span, { method: 'GET' }, {} as Response)
        expect(span.updateName).not.toHaveBeenCalled()
      })

      it('does not rename span when url is malformed', () => {
        const span = makeSpan('not-a-valid-url')
        cb(span, { method: 'GET' }, {} as Response)
        expect(span.updateName).not.toHaveBeenCalled()
      })
    })
  })
})
