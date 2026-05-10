import { ref } from 'vue'
import { FARO_CONSENT_KEY } from '@/plugins/faro'

const bannerVisible = ref(localStorage.getItem(FARO_CONSENT_KEY) === null)
const currentStatus = ref<string | null>(null)

export function useConsent () {
  function openPreferences () {
    const stored = localStorage.getItem(FARO_CONSENT_KEY)
    currentStatus.value = stored === 'true' ? 'Accepted' : stored === 'false' ? 'Declined' : null
    localStorage.removeItem(FARO_CONSENT_KEY)
    bannerVisible.value = true
  }

  return { bannerVisible, currentStatus, openPreferences }
}
