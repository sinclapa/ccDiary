<template>
  <v-app>
    <v-main>
      <router-view />
    </v-main>
  </v-app>
</template>

<script lang="ts" setup>
  //
</script>

<style>
  :root {
    --cc-map-height: 250px;
    --cc-header-height: 64px;
  }

  .map-component-placeholder {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    height: var(--cc-map-height);
    background: rgb(var(--v-theme-surface-variant));
    border-radius: 4px;
  }
</style>
