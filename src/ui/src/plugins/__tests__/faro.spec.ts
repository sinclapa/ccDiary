import { vi, describe, it, expect, beforeEach } from 'vitest'

const { mockInitializeFaro, mockGetWebInstrumentations, MockTracingInstrumentation, mockGetDefaultOTELInstrumentations } = vi.hoisted(() => ({
  mockInitializeFaro: vi.fn(),
  mockGetWebInstrumentations: vi.fn(() => []),
  MockTracingInstrumentation: vi.fn(),
  mockGetDefaultOTELInstrumentations: vi.fn((opts) => [{ name: 'mock-otel', options: opts }]),
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

import { initFaro } from '../faro'
import { getAppConfigField } from '@/utils/appConfig'

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

    function setupMocks(overrides: Record<string, string> = {}) {
      vi.mocked(getAppConfigField).mockImplementation((field, opts) => {
        const values: Record<string, string> = {
          VITE_FARO_URL: faroUrl,
          VITE_API: 'NOT_SET',
          VITE_APP_VERSION: '1.2.3',
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
      expect(config.app.version).toBe('1.2.3')
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

    it('beforeSend filters log events containing the dynamic import error message', () => {
      setupMocks()
      initFaro()
      const { beforeSend } = mockInitializeFaro.mock.calls[0][0]
      const logItem = {
        type: 'log',
        payload: { message: 'console.error: Dynamic import error Failed to fetch dynamically imported module: http://localhost:8080/src/pages/diaries/[id].vue' },
        meta: {},
      }
      expect(beforeSend(logItem)).toBeNull()
    })

    it('beforeSend passes through unrelated log events', () => {
      setupMocks()
      initFaro()
      const { beforeSend } = mockInitializeFaro.mock.calls[0][0]
      const logItem = {
        type: 'log',
        payload: { message: 'console.error: Some unrelated application error' },
        meta: {},
      }
      expect(beforeSend(logItem)).toBe(logItem)
    })

    it('beforeSend passes through non-log event types unmodified', () => {
      setupMocks()
      initFaro()
      const { beforeSend } = mockInitializeFaro.mock.calls[0][0]
      const exceptionItem = {
        type: 'exception',
        payload: { value: 'Failed to fetch dynamically imported module: http://localhost:8080/foo.vue', type: 'TypeError' },
        meta: {},
      }
      expect(beforeSend(exceptionItem)).toBe(exceptionItem)
    })
  })
})
