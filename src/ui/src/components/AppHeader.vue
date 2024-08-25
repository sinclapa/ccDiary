<template>
  <v-app-bar app>
    <v-app-bar-nav-icon @click.stop="drawer = !drawer"></v-app-bar-nav-icon>
    <v-app-bar-title>Cooking Code Diary</v-app-bar-title>
    <v-spacer></v-spacer>
    <div v-if="state.isAuthenticated" >{{ state.user?.name }}</div>
    <v-btn v-if="state.isAuthenticated" icon @click="handleLogout">
      <v-icon>mdi-account-circle</v-icon>
    </v-btn>
    <v-btn v-else icon @click="handleLogin">
      <v-icon>mdi-login</v-icon>
    </v-btn>
  </v-app-bar>
  <v-navigation-drawer
    v-model="drawer"
    :location="$vuetify.display.mobile ? 'bottom' : undefined"
    temporary
  >
    <v-list-item title="Cooking Code" subtitle="Diary"></v-list-item>
    <v-divider></v-divider>
    <v-list-item link title="Home" href="/"></v-list-item>
    <v-list-item link title="Diaries" href="/diaries"></v-list-item>
  </v-navigation-drawer>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { msalService } from '@/services/authentication/msalService';
import { state } from '@/services/authentication/msalConfig';

const drawer = ref(false)
const { initialize: initializeAuth, login, logout, handleRedirect, registerAuthorizationHeaderInterceptor } = msalService();

const handleLogin = async () => {
    await login();
};

const handleLogout = () => {
    logout();
};

const initialize = async () => {
    try {
        await initializeAuth();
    } catch (error) {
        console.log('Initialization error', error)
    }
};

onMounted(async () => {
    await initialize();
    await handleRedirect();
    await registerAuthorizationHeaderInterceptor();
})
</script>
