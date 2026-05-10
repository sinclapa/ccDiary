<template>
  <v-snackbar
    v-model="bannerVisible"
    location="bottom"
    :timeout="-1"
    multi-line
  >
    We use analytics to monitor performance and improve the app.
    No personal data is sold or shared.
    <template #actions>
      <v-btn variant="text" @click="decline">Decline</v-btn>
      <v-btn color="primary" variant="tonal" @click="accept">Accept</v-btn>
    </template>
  </v-snackbar>
</template>

<script lang="ts" setup>
  import { useConsent } from '@/composables/useConsent'
  import { FARO_CONSENT_KEY, initFaro } from '@/plugins/faro'

  const { bannerVisible } = useConsent()

  function accept () {
    localStorage.setItem(FARO_CONSENT_KEY, 'true')
    initFaro()
    bannerVisible.value = false
  }

  function decline () {
    localStorage.setItem(FARO_CONSENT_KEY, 'false')
    bannerVisible.value = false
  }
</script>
