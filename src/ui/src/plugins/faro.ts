import { getWebInstrumentations, initializeFaro, TransportItemType } from '@grafana/faro-web-sdk'
import type { TransportItem } from '@grafana/faro-core'
import { getDefaultOTELInstrumentations, TracingInstrumentation } from '@grafana/faro-web-tracing'
import { getAppConfigField } from '@/utils/appConfig'

const DYNAMIC_IMPORT_ERROR = /Failed to fetch dynamically imported module/

export function initFaro () {
  const url = getAppConfigField('VITE_FARO_URL')
  if (!url || url === 'NOT_SET') return

  const apiUrl = getAppConfigField('VITE_API')
  const propagateUrls =
    apiUrl && apiUrl !== 'NOT_SET'
      ? [new RegExp(apiUrl.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'))]
      : []

  initializeFaro({
    url,
    app: {
      name: 'ccdiary-ui',
      version: __APP_VERSION__,
      environment: getAppConfigField('VITE_ENVIRONMENT', { defaultValue: 'unknown' }),
    },
    // Router handles dynamic import failures with a reload — suppress exception events
    ignoreErrors: [DYNAMIC_IMPORT_ERROR],
    // Suppress log events (console.error captures) with the same pattern
    beforeSend: (item: TransportItem) => {
      if (item.type === TransportItemType.LOG) {
        const payload = item.payload as { message?: string }
        if (payload.message && DYNAMIC_IMPORT_ERROR.test(payload.message)) {
          return null
        }
      }
      return item
    },
    instrumentations: [
      ...getWebInstrumentations(),
      new TracingInstrumentation({
        instrumentations: getDefaultOTELInstrumentations({
          ignoreUrls: [/\.vue(\?|$)/, /\/@vite\//, /\/@fs\//, /\/node_modules\//],
          propagateTraceHeaderCorsUrls: propagateUrls,
          fetchInstrumentationOptions: {
            applyCustomAttributesOnSpan: (span, request) => {
              const attrs = (span as unknown as { attributes?: Record<string, unknown> }).attributes
              const rawUrl = String(attrs?.['url.full'] ?? attrs?.['http.url'] ?? '')
              if (!rawUrl) return
              let pathname: string
              try {
                pathname = new URL(rawUrl).pathname
              } catch {
                return
              }
              const normalizedPath = pathname.replace(
                /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi,
                '{id}',
              )
              const method = ((request as RequestInit).method ?? 'GET').toUpperCase()
              span.updateName(`${method} ${normalizedPath}`)
            },
          },
        }),
      }),
    ],
  })
}
