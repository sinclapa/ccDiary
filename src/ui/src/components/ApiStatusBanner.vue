<template>
  <Transition name="api-status-fade">
    <div
      v-if="!apiStatus.available"
      aria-live="polite"
      class="api-status-bar"
      role="status"
    >
      <span aria-hidden="true" class="api-status-dot" />
      <span>Preparing the ingredients, please wait...</span>
      <span class="wait-counter">({{ elapsedSeconds }}s)</span>
    </div>
  </Transition>
</template>

<script lang="ts" setup>
  import { useApiStatusStore } from '@/stores/apiStatus'

  const apiStatus = useApiStatusStore()

  const elapsedSeconds = ref(0)
  let ticker: ReturnType<typeof setInterval> | null = null

  function startCounter () {
    elapsedSeconds.value = 0
    ticker = setInterval(() => { elapsedSeconds.value++ }, 1000)
  }

  function stopCounter () {
    if (ticker) {
      clearInterval(ticker)
      ticker = null
    }
  }

  watch(() => apiStatus.available, isAvailable => {
    if (isAvailable) {
      stopCounter()
    } else {
      startCounter()
    }
  })

  onMounted(() => {
    apiStatus.registerFetchInterceptor()
    apiStatus.checkHealth()
  })

  onUnmounted(() => {
    stopCounter()
  })
</script>

<style scoped>
  /* Fixed rather than in the page flow, so appearing or clearing never shifts the content
     below it; pointer-events off so it never swallows a click on a control underneath. */
  .api-status-bar {
    position: fixed;
    top: calc(var(--cc-header-height) + 8px);
    left: 50%;
    transform: translateX(-50%);
    z-index: 999;
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 4px 12px;
    border-radius: 999px;
    border: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
    background: rgb(var(--v-theme-surface));
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.12);
    color: rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity));
    font-size: 0.75rem;
    white-space: nowrap;
    pointer-events: none;
  }

  .api-status-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    background: rgb(var(--v-theme-warning));
    animation: api-status-pulse 1.6s ease-in-out infinite;
  }

  @keyframes api-status-pulse {
    0%, 100% { opacity: 0.35; }
    50% { opacity: 1; }
  }

  .wait-counter {
    font-variant-numeric: tabular-nums;
  }

  .api-status-fade-enter-active,
  .api-status-fade-leave-active {
    transition: opacity 0.2s ease, transform 0.2s ease;
  }

  .api-status-fade-enter-from,
  .api-status-fade-leave-to {
    opacity: 0;
    transform: translate(-50%, -4px);
  }

  @media (max-width: 599px) {
    .api-status-bar {
      top: 112px;
    }
  }

  @media (prefers-reduced-motion: reduce) {
    .api-status-dot {
      animation: none;
      opacity: 1;
    }
  }
</style>
