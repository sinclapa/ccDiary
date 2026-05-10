<template>
  <v-app-bar app>
    <v-app-bar-nav-icon aria-label="Open navigation" @click.stop="drawer = !drawer" />
    <v-app-bar-title style="flex-shrink: 0">Cooking Code Diary</v-app-bar-title>
    <div v-if="state.isAuthenticated" id="username" class="text-truncate mr-2 d-none d-sm-block" style="min-width: 0">{{ state.user?.name }}</div>
    <v-btn
      id="theme-toggle"
      icon
      :aria-label="isDark ? 'Switch to light theme' : 'Switch to dark theme'"
      @click="toggleTheme"
    >
      <v-icon>{{ themeIcon }}</v-icon>
    </v-btn>
    <v-btn
      v-if="state.isAuthenticated"
      id="logout"
      v-tooltip="state.user?.name"
      icon
      @click="handleLogout"
    >
      <v-icon>$mdi-account-circle</v-icon>
    </v-btn>
    <v-btn v-else id="login" icon @click="handleLogin">
      <v-icon>$mdi-login</v-icon>
    </v-btn>
  </v-app-bar>
  <v-navigation-drawer
    v-model="drawer"
    :location="$vuetify.display.mobile ? 'top' : undefined"
    temporary
  >
    <v-list-item subtitle="Diary" title="Cooking Code" />
    <v-divider />
    <v-list-item link title="Home" to="/" />
    <v-list-item link title="Diaries" to="/diaries" />
    <v-list-item v-if="!state.isAuthenticated" link title="Register" to="/register" />
    <v-list-item v-if="authStore.isAdmin" link title="Admin" to="/admin" />
  </v-navigation-drawer>
</template>

<script setup lang="ts">
  import { computed, onMounted } from 'vue'
  import { useTheme } from 'vuetify'
  import { useRouter } from 'vue-router'
  import { msalService } from '@/services/authentication/msalService'
  import { state } from '@/services/authentication/msalConfig'
  import { useAuthStore } from '@/stores/auth'
  import { saveTheme } from '@/utils/browserTheme'

  const drawer = ref(false)
  const router = useRouter()
  const theme = useTheme()
  const isDark = computed(() => theme.global.name.value === 'dark')
  const themeIcon = computed(() => isDark.value ? '$mdi-weather-sunny' : '$mdi-weather-night')

  const toggleTheme = () => {
    const next = isDark.value ? 'light' : 'dark'
    theme.global.name.value = next
    saveTheme(next)
  }
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
    await initialize()
    const redirectPath = await handleRedirect()
    if (redirectPath) await router.replace(redirectPath)
    await registerAuthorizationHeaderInterceptor()
    if (state.isAuthenticated) {
      await authStore.fetchAppUser()
    }
  })
</script>
