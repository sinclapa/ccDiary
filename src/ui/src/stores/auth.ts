import { defineStore } from 'pinia'
import type { AppUser } from '@/services/models/appUser'
import { getMe } from '@/services/modules/userService'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    appUser: null as AppUser | null,
  }),
  getters: {
    isAdmin: state => state.appUser?.role === 'diary-admin',
    isContributor: state => state.appUser?.role === 'diary-admin' || state.appUser?.role === 'diary-contributor',
  },
  actions: {
    async fetchAppUser () {
      this.appUser = await getMe()
    },
    clearAppUser () {
      this.appUser = null
    },
  },
})
