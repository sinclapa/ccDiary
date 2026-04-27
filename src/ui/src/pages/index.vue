<template>
  <header class="text-center">
    <v-img
      alt="Cooking Code Diary"
      class="mx-auto my-4"
      max-width="200"
      :src="logo"
    />

    <h1>Cooking Code Diary App</h1>

    <p>Welcome to the Cooking Code Diary App! This is your personal space to document and share your adventures.</p>
    <p>Get started by creating a new diary entry, where you can jot down your thoughts</p>
  </header>
</template>

<script lang="ts" setup>
  import { computed } from 'vue'
  import { useTheme } from 'vuetify'
  import logoLight from '@/assets/logo-light.svg'
  import logoDark from '@/assets/logo-dark.svg'

  const theme = useTheme()
  const logo = computed(() => theme.global.name.value === 'dark' ? logoDark : logoLight)
</script>
