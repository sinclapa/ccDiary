import { defineStore } from 'pinia'
import { getAppConfigField } from '@/utils/appConfig'

export const useApiStatusStore = defineStore('apiStatus', {
  state: () => ({
    available: true,
    checking: false,
    pollTimer: null as ReturnType<typeof setInterval> | null,
    interceptorRegistered: false,
  }),

  actions: {
    async checkHealth () {
      this.checking = true
      try {
        const apiBase = new URL(getAppConfigField('VITE_API'))
        const url = new URL('/actuator/health', apiBase.origin)
        const response = await fetch(url, { signal: AbortSignal.timeout(5000) })
        this.setAvailable(response.ok)
      } catch {
        this.setAvailable(false)
      } finally {
        this.checking = false
      }
    },

    setAvailable (value: boolean) {
      const wasUnavailable = !this.available
      this.available = value
      if (!value) {
        this.startPolling()
      } else {
        this.stopPolling()
        if (wasUnavailable) {
          window.location.reload()
        }
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

      const originalFetch = window.fetch
      const store = this
      window.fetch = async (...args) => {
        try {
          const response = await originalFetch(...args)
          const [resource] = args
          if (resource.toString().includes(getAppConfigField('VITE_API'))) {
            store.setAvailable(true)
          }
          return response
        } catch (error) {
          const [resource] = args
          if (resource.toString().includes(getAppConfigField('VITE_API'))) {
            store.setAvailable(false)
          }
          throw error
        }
      }
    },
  },
})
