<template>
  <div v-if="!apiStatus.available" class="api-status-bar">
    <v-icon icon="$mdi-server-network-off" size="small" />
    <span class="spotlight-text ml-2">
      <span
        v-for="(char, i) in messageChars"
        :key="i"
        class="spotlight-char"
        :style="{ animationDelay: `${i * 0.06}s` }"
      >{{ char === ' ' ? ' ' : char }}</span>
    </span>
    <span class="ml-2 wait-counter">({{ elapsedSeconds }}s)</span>
  </div>
</template>

<script lang="ts" setup>
  import { useApiStatusStore } from '@/stores/apiStatus'

  const apiStatus = useApiStatusStore()
  const message = 'Preparing the ingredients, please wait...'
  const messageChars = message.split('')

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
  .api-status-bar {
    display: flex;
    align-items: center;
    padding: 6px 16px;
    background: rgba(var(--v-theme-warning), 0.15);
    border-bottom: 1px solid rgba(var(--v-theme-warning), 0.4);
    color: rgb(var(--v-theme-on-surface));
    font-size: 0.875rem;
    position: sticky;
    top: 0;
    z-index: 100;
    max-width: 1100px;
    width: 100%;
    margin: 0 auto;
  }

  .spotlight-text {
    display: inline-flex;
    flex-wrap: wrap;
  }

  .spotlight-char {
    animation: spotlight 2.5s ease-in-out infinite;
    opacity: 0.4;
  }

  @keyframes spotlight {
    0%, 100% { opacity: 0.4; text-shadow: none; }
    20%, 30% { opacity: 1; text-shadow: none; }
  }

  .wait-counter {
    font-variant-numeric: tabular-nums;
    opacity: 0.8;
  }
</style>
