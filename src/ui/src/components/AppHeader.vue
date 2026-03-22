<template>
  <v-app-bar app>
    <v-app-bar-nav-icon @click.stop="drawer = !drawer" />
    <v-app-bar-title>Cooking Code Diary</v-app-bar-title>
    <v-spacer />
    <div v-if="state.isAuthenticated" id="username" class="text-no-wrap text-truncate mr-2 d-none d-sm-block">{{ state.user?.name }}</div>
    <v-btn v-if="state.isAuthenticated" id="logout" v-tooltip="state.user?.name" icon @click="handleLogout">
      <v-icon>mdi-account-circle</v-icon>
    </v-btn>
    <v-btn v-else id="login" icon @click="handleLogin">
      <v-icon>mdi-login</v-icon>
    </v-btn>
  </v-app-bar>
  <v-navigation-drawer
    v-model="drawer"
    :location="$vuetify.display.mobile ? 'bottom' : undefined"
    temporary
  >
    <v-list-item subtitle="Diary" title="Cooking Code" />
    <v-divider />
    <v-list-item href="/" link title="Home" />
    <v-list-item href="/diaries" link title="Diaries" />
  </v-navigation-drawer>
</template>

<script setup lang="ts">
  import { onMounted } from 'vue'
  import { msalService } from '@/services/authentication/msalService'
  import { state } from '@/services/authentication/msalConfig'

  const drawer = ref(false)
  const { initializeInstance, login, logout, handleRedirect, registerAuthorizationHeaderInterceptor } = msalService()

  const handleLogin = async () => {
    await login()
  }

  const handleLogout = () => {
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
    await handleRedirect()
    await registerAuthorizationHeaderInterceptor()
  })
</script>
