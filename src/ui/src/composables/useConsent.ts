import { ref } from 'vue'
import { FARO_CONSENT_KEY } from '@/plugins/faro'

const bannerVisible = ref(localStorage.getItem(FARO_CONSENT_KEY) === null)
const currentStatus = ref<string | null>(null)

// what a stored consent value means to the reader; anything else means no choice yet
const CONSENT_LABELS: Record<string, string> = { true: 'Accepted', false: 'Declined' }

export function useConsent () {
  function openPreferences () {
    const stored = localStorage.getItem(FARO_CONSENT_KEY)
    currentStatus.value = CONSENT_LABELS[stored ?? ''] ?? null
    localStorage.removeItem(FARO_CONSENT_KEY)
    bannerVisible.value = true
  }

  return { bannerVisible, currentStatus, openPreferences }
}
