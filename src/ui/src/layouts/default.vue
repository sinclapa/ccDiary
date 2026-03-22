<template>
  <v-app>
    <v-layout>
      <AppHeader />

      <v-main>
        <ApiStatusBanner />
        <router-view />
      </v-main>

      <AppFooter />
    </v-layout>
  </v-app>
</template>

<script lang="ts" setup>
  //
</script>
