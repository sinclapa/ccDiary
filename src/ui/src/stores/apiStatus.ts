import { defineStore } from 'pinia'
import { getAppConfigField } from '@/utils/appConfig'

/** How long the API may stay silent before the UI admits something is happening. */
const SHOW_WAKING_AFTER_MS = 1500

/**
 * Upper bound on a single health probe. Generous on purpose: the API scales to zero and a
 * cold start takes tens of seconds, so a short timeout would abort the very request that is
 * waking the container and the retry would start the wait over again.
 */
const HEALTH_TIMEOUT_MS = 90_000

/** Gateway responses that mean "the container is not up yet", not "the request failed". */
const UPSTREAM_UNAVAILABLE = new Set([502, 503, 504])

export const useApiStatusStore = defineStore('apiStatus', {
  state: () => ({
    available: true,
    checking: false,
    pollTimer: null as ReturnType<typeof setInterval> | null,
    interceptorRegistered: false,
    recoveryCount: 0,
  }),

  actions: {
    async checkHealth () {
      // Polling fires on a fixed interval while a probe may still be in flight against a
      // starting container. Without this guard those pile up, each one competing for the
      // replica's attention and none of them finishing sooner.
      if (this.checking) return

      this.checking = true

      // Surface the wait rather than waiting on a failure to surface it: the request is
      // left running, and the banner appears alongside it.
      const revealTimer = setTimeout(() => {
        if (this.checking) this.setAvailable(false)
      }, SHOW_WAKING_AFTER_MS)

      try {
        this.setAvailable(await this.probe())
      } catch {
        this.setAvailable(false)
      } finally {
        clearTimeout(revealTimer)
        this.checking = false
      }
    },

    /**
     * Resolves true when the API answers healthily. Reuses the warm-up request started in
     * index.html for the first probe, so the app does not issue a second one behind it.
     */
    async probe (): Promise<boolean> {
      const warmup = (globalThis as Record<string, unknown>).__ccdiaryApiWarmup as
        | Promise<boolean>
        | undefined

      if (warmup) {
        delete (globalThis as Record<string, unknown>).__ccdiaryApiWarmup
        return warmup
      }

      const apiBase = new URL(getAppConfigField('VITE_API'))
      const url = new URL('/actuator/health', apiBase.origin)
      const response = await fetch(url, { signal: AbortSignal.timeout(HEALTH_TIMEOUT_MS) })
      return response.ok
    },

    setAvailable (value: boolean) {
      const wasUnavailable = !this.available
      this.available = value
      if (value) {
        this.stopPolling()
        if (wasUnavailable) {
          this.recoveryCount++
        }
      } else {
        this.startPolling()
      }
    },

    startPolling () {
      if (this.pollTimer) return
      this.pollTimer = setInterval(() => this.checkHealth(), 5000)
    },

    stopPolling () {
      if (this.pollTimer) {
        clearInterval(this.pollTimer)
        this.pollTimer = null
      }
    },

    registerFetchInterceptor () {
      if (this.interceptorRegistered) return
      this.interceptorRegistered = true

      const originalFetch = globalThis.fetch
      globalThis.fetch = async (...args) => {
        const [resource] = args
        const resourceUrl = resource instanceof Request ? resource.url : resource.toString()
        const isApiCall = resourceUrl.includes(getAppConfigField('VITE_API'))

        try {
          const response = await originalFetch(...args)
          // A cold container answers through the ingress, which returns a gateway error
          // rather than throwing. Treated as a success this looks like the API returning
          // nonsense; treated as down it correctly reads as "still starting".
          if (isApiCall && UPSTREAM_UNAVAILABLE.has(response.status)) {
            this.setAvailable(false)
          }
          return response
        } catch (error) {
          if (isApiCall) {
            this.setAvailable(false)
          }
          throw error
        }
      }
    },
  },
})
