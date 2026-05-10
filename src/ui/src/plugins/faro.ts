import { getWebInstrumentations, initializeFaro, TransportItemType } from '@grafana/faro-web-sdk'
import type { Faro, TransportItem } from '@grafana/faro-core'
import type { UserActionInternalInterface } from '@grafana/faro-core/dist/bundle/types/api/userActions/types'
import { getDefaultOTELInstrumentations, TracingInstrumentation } from '@grafana/faro-web-tracing'
import { getAppConfigField } from '@/utils/appConfig'

export const FARO_CONSENT_KEY = 'faro-consent'

let faroInstance: Faro | undefined
let currentUserAction: UserActionInternalInterface | undefined

const DYNAMIC_IMPORT_ERROR = /Failed to fetch dynamically imported module/

export function initFaro () {
  faroInstance = undefined
  const url = getAppConfigField('VITE_FARO_URL')
  if (!url || url === 'NOT_SET') return
  if (localStorage.getItem(FARO_CONSENT_KEY) !== 'true') return

  const apiUrl = getAppConfigField('VITE_API')
  const propagateUrls =
    apiUrl && apiUrl !== 'NOT_SET'
      ? [new RegExp(apiUrl.replaceAll(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`))]
      : []

  const collectorUrlPattern = new RegExp(url.replaceAll(/[.*+?^${}()|[\]\\]/g, String.raw`\$&`))

  faroInstance = initializeFaro({
    url,
    app: {
      name: 'ccdiary-ui',
      version: __APP_VERSION__,
      environment: getAppConfigField('VITE_ENVIRONMENT', { defaultValue: 'unknown' }),
    },
    // Router handles dynamic import failures with a reload — suppress exception events
    ignoreErrors: [DYNAMIC_IMPORT_ERROR],
    // Suppress log events (console.error captures) matching the dynamic import error
    // pattern or containing the Faro collector URL
    beforeSend: (item: TransportItem) => {
      if (item.type === TransportItemType.LOG) {
        const payload = item.payload as { message?: string }
        if (payload.message && (DYNAMIC_IMPORT_ERROR.test(payload.message) || collectorUrlPattern.test(payload.message))) {
          return null
        }
      }
      return item
    },
    sessionTracking: {
      enabled: true,
      persistent: true,
    },
    instrumentations: [
      ...getWebInstrumentations(),
      new TracingInstrumentation({
        instrumentations: getDefaultOTELInstrumentations({
          ignoreUrls: [/\.vue(\?|$)/, /\/@vite\//, /\/@fs\//, /\/node_modules\//, collectorUrlPattern],
          propagateTraceHeaderCorsUrls: propagateUrls,
          fetchInstrumentationOptions: {
            applyCustomAttributesOnSpan: (span, request) => {
              const attrs = (span as unknown as { attributes?: Record<string, string> }).attributes
              const rawUrl = attrs?.['url.full'] ?? attrs?.['http.url'] ?? ''
              if (!rawUrl) return
              let pathname: string
              try {
                pathname = new URL(rawUrl).pathname
              } catch {
                return
              }
              const normalizedPath = pathname.replaceAll(
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

export function pushFaroEvent (name: string, attributes?: Record<string, string>) {
  faroInstance?.api.pushEvent(name, attributes)
}

export function startFaroUserAction (name: string, attributes?: Record<string, string>) {
  currentUserAction = faroInstance?.api.startUserAction(name, attributes ?? {}) as UserActionInternalInterface | undefined
}

export function endFaroUserAction () {
  currentUserAction?.end()
  currentUserAction = undefined
}
