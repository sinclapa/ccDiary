<template>
  <header class="app-header" :style="headerStyle">
    <div class="app-header__bar">
      <router-link class="logo-link ml-4" to="/">
        <span class="logo-cc">cc</span><span class="logo-diary">Diary</span>
      </router-link>

      <nav class="desktop-nav">
        <v-btn to="/" variant="text">Home</v-btn>
        <v-btn to="/diaries" variant="text">Diaries</v-btn>
        <v-btn v-if="authStore.isAdmin" to="/admin" variant="text">Admin</v-btn>
      </nav>

      <v-spacer />

      <div v-if="state.isAuthenticated" id="username" class="text-truncate mr-2 d-none d-sm-block" style="min-width: 0">{{ state.user?.name }}</div>
      <v-btn
        id="theme-toggle"
        :aria-label="isDark ? 'Switch to light theme' : 'Switch to dark theme'"
        icon
        @click="toggleTheme"
      >
        <v-icon>{{ themeIcon }}</v-icon>
      </v-btn>
      <div v-if="state.isAuthenticated" class="tooltip-wrap">
        <v-btn id="logout" icon @click="handleLogout">
          <v-icon>$mdi-account-circle</v-icon>
        </v-btn>
        <span class="user-tooltip">Logout {{ state.user?.name }}</span>
      </div>
      <v-btn v-else id="login" icon @click="handleLogin">
        <v-icon>$mdi-login</v-icon>
      </v-btn>
    </div>

    <nav class="mobile-nav px-2 pb-1">
      <v-btn size="small" to="/" variant="text">Home</v-btn>
      <v-btn size="small" to="/diaries" variant="text">Diaries</v-btn>
      <v-btn v-if="authStore.isAdmin" size="small" to="/admin" variant="text">Admin</v-btn>
    </nav>
  </header>
</template>

<script setup lang="ts">
  import { computed, onMounted, onUnmounted, ref } from 'vue'
  import { useDisplay, useTheme } from 'vuetify'
  import { useRouter } from 'vue-router'
  import { msalService } from '@/services/authentication/msalService'
  import { state } from '@/services/authentication/msalConfig'
  import { useAuthStore } from '@/stores/auth'
  import { saveTheme } from '@/utils/browserTheme'

  const router = useRouter()
  const theme = useTheme()
  const { mobile } = useDisplay()
  const isDark = computed(() => theme.global.name.value === 'dark')
  const themeIcon = computed(() => isDark.value ? '$mdi-weather-sunny' : '$mdi-weather-night')

  const toggleTheme = () => {
    const next = isDark.value ? 'light' : 'dark'
    theme.global.name.value = next
    saveTheme(next)
  }

  const headerHidden = ref(false)
  let lastScrollY = 0

  function onScroll () {
    if (window.innerWidth > 640) { headerHidden.value = false; return }
    const y = window.scrollY
    headerHidden.value = y > lastScrollY && y > 80
    lastScrollY = y
  }

  // backdrop-filter is on ::before (not the header itself) to avoid breaking
  // position:fixed in Chrome when backdrop-filter is on the fixed element directly.
  // No transform on desktop — applying transform:translateY(0) also breaks fixed positioning.
  const headerStyle = computed(() => {
    const style: Record<string, string> = {
      background: 'rgba(var(--v-theme-background), 0.75)',
    }
    if (mobile.value) {
      style.transition = 'transform 0.3s ease'
      if (headerHidden.value) {
        style.transform = 'translateY(-110%)'
      }
    }
    return style
  })

  const authStore = useAuthStore()
  const { initializeInstance, login, logout, handleRedirect, registerAuthorizationHeaderInterceptor } = msalService()

  const handleLogin = async () => {
    await login()
    if (state.isAuthenticated) {
      await authStore.fetchAppUser()
    }
  }

  const handleLogout = () => {
    authStore.clearAppUser()
    logout()
  }

  const initialize = async () => {
    try {
      await initializeInstance()
    } catch (error) {
      console.log('Initialization error', error)
    }
  }

  onMounted(async () => {
    window.addEventListener('scroll', onScroll, { passive: true })
    await initialize()
    const redirectPath = await handleRedirect()
    if (redirectPath) await router.replace(redirectPath)
    await registerAuthorizationHeaderInterceptor()
    if (state.isAuthenticated) {
      await authStore.fetchAppUser()
    }
  })

  onUnmounted(() => {
    window.removeEventListener('scroll', onScroll)
  })
</script>

<style scoped>
.app-header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1000;
  border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
}

/* Blur via ::before so backdrop-filter is NOT on the fixed element itself,
   avoiding the Chrome bug where backdrop-filter on position:fixed breaks stickiness. */
.app-header::before {
  content: '';
  position: absolute;
  inset: 0;
  z-index: -1;
  -webkit-backdrop-filter: blur(16px) saturate(180%);
  backdrop-filter: blur(16px) saturate(180%);
  pointer-events: none;
}

.app-header__bar {
  position: relative;
  display: flex;
  align-items: center;
  width: 100%;
  max-width: 1100px;
  height: 64px;
  box-sizing: border-box;
  padding-right: 8px;
  margin: 0 auto;
}

.logo-link {
  text-decoration: none;
  display: flex;
  align-items: center;
  font-size: 1.25rem;
  font-weight: 700;
  flex-shrink: 0;
}

.logo-cc {
  color: rgb(var(--v-theme-on-background));
}

.logo-diary {
  color: rgb(var(--v-theme-primary));
}

/* Desktop nav: absolutely centred in the bar so logo and controls stay at edges */
.desktop-nav {
  position: absolute;
  left: 50%;
  top: 50%;
  transform: translate(-50%, -50%);
  display: flex;
  align-items: center;
}

/* Active route item — orange text, keep default grey background tint */
.desktop-nav :deep(.v-btn--active),
.mobile-nav :deep(.v-btn--active) {
  color: rgb(var(--v-theme-primary)) !important;
}

/* Mobile nav: hidden by default, visible on mobile */
.mobile-nav {
  display: none;
}

.tooltip-wrap {
  position: relative;
  display: inline-flex;
}

.user-tooltip {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  background: rgb(var(--v-theme-primary));
  color: #fff;
  padding: 4px 10px;
  border-radius: 4px;
  font-size: 0.75rem;
  white-space: nowrap;
  pointer-events: none;
  opacity: 0;
  transition: opacity 0.15s ease;
  z-index: 1001;
}

.tooltip-wrap:hover .user-tooltip {
  opacity: 1;
}

@media (max-width: 599px) {
  .desktop-nav {
    display: none;
  }

  .mobile-nav {
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 0.25rem;
    height: 40px;
  }
}
</style>
