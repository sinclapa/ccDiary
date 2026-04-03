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
  import logo from '@/assets/logo.png'
</script>
