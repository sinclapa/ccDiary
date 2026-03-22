<template>
  <v-banner
    v-if="!apiStatus.available"
    color="warning"
    icon="mdi-server-network-off"
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
      <v-icon class="ml-2 spin-icon" size="18">mdi-loading</v-icon>
    </v-banner-text>
  </v-banner>
</template>

<script lang="ts" setup>
  import { useApiStatusStore } from '@/stores/apiStatus'

  const apiStatus = useApiStatusStore()
  const message = 'The ingredients are being prepared, please wait...'
  const messageChars = message.split('')

  onMounted(() => {
    apiStatus.registerFetchInterceptor()
    apiStatus.checkHealth()
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
    20%, 30% { opacity: 1; text-shadow: 0 0 8px currentColor; }
  }

  .spin-icon {
    animation: spin 1s linear infinite;
  }

  @keyframes spin {
    from { transform: rotate(0deg); }
    to { transform: rotate(360deg); }
  }
</style>
