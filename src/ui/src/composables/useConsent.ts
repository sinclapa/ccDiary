import { ref } from 'vue'
import { FARO_CONSENT_KEY } from '@/plugins/faro'

const bannerVisible = ref(localStorage.getItem(FARO_CONSENT_KEY) === null)

export function useConsent () {
  function openPreferences () {
    localStorage.removeItem(FARO_CONSENT_KEY)
    bannerVisible.value = true
  }

  return { bannerVisible, openPreferences }
}
