<template>
  <v-banner
    v-if="!apiStatus.available"
    color="warning"
    icon="$mdi-server-network-off"
    lines="one"
    sticky
  >
    <v-banner-text>
      <span class="spotlight-text">
        <span
          v-for="(char, i) in messageChars"
          :key="i"
          class="spotlight-char"
          :style="{ animationDelay: `${i * 0.06}s` }"
        >{{ char === ' ' ? '\u00A0' : char }}</span>
      </span>
      <span class="ml-2 wait-counter">({{ elapsedSeconds }}s)</span>
    </v-banner-text>
  </v-banner>
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
