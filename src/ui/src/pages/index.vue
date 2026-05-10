<template>
  <header class="text-center">
    <v-img
      alt="Cooking Code Diary"
      class="mx-auto my-4"
      max-width="200"
      :src="logo"
    />

    <h1 class="hero-title">
      <span class="hero-title__line">
        <span class="hero-title__cc">Cooking</span>
        <span class="hero-title__diary">Code</span>
      </span>
      <span class="hero-title__line hero-title__line--second hero-title__diary">Diary</span>
    </h1>

    <p>Welcome to the Cooking Code Diary App! This is your personal space to document and share your adventures.</p>
    <p>Get started by creating a new diary entry, where you can jot down your thoughts, travels or experiences.</p>
  </header>
</template>

<script lang="ts" setup>
  import { computed } from 'vue'
  import { useTheme } from 'vuetify'
  import logoLight from '@/assets/logo-simple-light.svg'
  import logoDark from '@/assets/logo-simple-dark.svg'

  const theme = useTheme()
  const logo = computed(() => theme.global.name.value === 'dark' ? logoDark : logoLight)
</script>

<style scoped>
.hero-title {
  display: inline-flex;
  flex-direction: column;
  line-height: 1.1;
  margin-bottom: 0.75rem;
}

.hero-title__line {
  display: block;
}

.hero-title__line--second {
  margin-top: 0.1rem;
}

.hero-title__cc {
  color: rgb(var(--v-theme-on-background));
}

.hero-title__diary {
  color: rgb(var(--v-theme-primary));
}
</style>
