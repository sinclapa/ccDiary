<template>
  <Teleport to="body">
    <div v-if="bannerVisible" aria-label="Cookie consent" class="consent-banner" role="dialog">
      <p class="consent-text">
        We use analytics to monitor performance and improve the app.
        No personal data is sold or shared.
      </p>
      <div class="consent-actions">
        <span v-if="currentStatus" class="consent-current-status">Current: {{ currentStatus }}</span>
        <button
          id="consent-decline"
          class="consent-btn consent-btn--decline"
          :class="{ 'consent-btn--current': currentStatus === 'Declined' }"
          @click="decline"
        >Decline</button>
        <button
          id="consent-accept"
          class="consent-btn consent-btn--accept"
          :class="{ 'consent-btn--current': currentStatus === 'Accepted' }"
          @click="accept"
        >Accept</button>
      </div>
    </div>
  </Teleport>
</template>

<script lang="ts" setup>
  import { useConsent } from '@/composables/useConsent'
  import { FARO_CONSENT_KEY, initFaro } from '@/plugins/faro'

  const { bannerVisible, currentStatus } = useConsent()

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

<style scoped>
.consent-banner {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1.5rem;
  padding: 1rem 2rem;
  background: rgb(var(--v-theme-surface-variant));
  border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  font-size: 0.875rem;
  color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
}

.consent-text {
  margin: 0;
  flex: 1;
}

.consent-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-shrink: 0;
}

.consent-btn {
  padding: 0.4rem 1rem;
  border-radius: 6px;
  font-size: 0.875rem;
  font-weight: 500;
  cursor: pointer;
  border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
  transition: background 0.15s, color 0.15s, border-color 0.15s;
}

.consent-btn--decline {
  background: transparent;
  color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
}

.consent-btn--decline:hover {
  color: rgb(var(--v-theme-on-surface));
  border-color: rgba(var(--v-theme-on-surface), 0.4);
}

.consent-btn--accept {
  background: rgb(var(--v-theme-primary));
  color: rgb(var(--v-theme-on-primary));
  border-color: rgb(var(--v-theme-primary));
}

.consent-btn--accept:hover {
  background: rgb(var(--v-theme-primary-darken-1));
  border-color: rgb(var(--v-theme-primary-darken-1));
}

.consent-current-status {
  font-size: 0.75rem;
  color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
  white-space: nowrap;
  font-style: italic;
}

.consent-btn--current {
  box-shadow: 0 0 0 2px rgb(var(--v-theme-surface-variant)), 0 0 0 4px rgb(var(--v-theme-primary));
}

.consent-btn--decline.consent-btn--current {
  background: rgba(var(--v-theme-on-surface), 0.1);
  color: rgb(var(--v-theme-on-surface));
  border-color: rgba(var(--v-theme-on-surface), 0.4);
}

@media (max-width: 640px) {
  .consent-banner {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }
}
</style>
