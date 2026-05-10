<template>
  <v-app>
    <AppHeader />

    <main class="page-body">
      <ConsentBanner />
      <ApiStatusBanner />
      <div class="page-content">
        <router-view />
      </div>
      <AppFooter />
    </main>
  </v-app>
</template>

<script lang="ts" setup>
  //
</script>

<style scoped>
.page-body {
  display: flex;
  flex-direction: column;
  flex: 1 1 auto;
  min-height: 100vh;
  padding-top: 64px;
  background: rgb(var(--v-theme-background));
  color: rgb(var(--v-theme-on-background));
}

.page-content {
  flex: 1;
  max-width: 1100px;
  width: 100%;
  margin: 0 auto;
}

@media (max-width: 599px) {
  .page-body {
    padding-top: 104px;
  }
}
</style>
