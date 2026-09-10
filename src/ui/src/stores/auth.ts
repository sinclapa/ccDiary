import { defineStore } from 'pinia'
import type { AppUser } from '@/services/models/appUser'
import { getMe } from '@/services/modules/userService'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    appUser: null as AppUser | null,
    /** Set when the last lookup could not reach the API, so it is worth trying again. */
    appUserUnavailable: false,
  }),
  getters: {
    isAdmin: state => state.appUser?.role === 'diary-admin',
    isContributor: state => state.appUser?.role === 'diary-admin' || state.appUser?.role === 'diary-contributor',
  },
  actions: {
    async fetchAppUser () {
      try {
        this.appUser = await getMe()
        this.appUserUnavailable = false
      } catch {
        // Leave whatever is already known in place. Overwriting it with null on a cold
        // start silently demoted the signed-in user to no role at all, with nothing to
        // put it back.
        this.appUserUnavailable = true
      }
    },
    clearAppUser () {
      this.appUser = null
      this.appUserUnavailable = false
    },
  },
})
