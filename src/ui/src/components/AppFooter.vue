<template>
  <v-footer app height="40">
    <div class="text-caption text-disabled">
      <span>Version {{ version }}</span>
    </div>
    <a
      v-for="item in items"
      :key="item.title"
      class="d-inline-block mx-2 social-link"
      :href="item.href"
      rel="noopener noreferrer"
      target="_blank"
      :title="item.title"
    >
      <v-icon
        :icon="item.icon"
        :size="item.icon === '$vuetify' ? 24 : 16"
      />
    </a>

    <div
      class="text-caption text-disabled"
      style="position: absolute; right: 16px;"
    >
      &copy; 2023-{{ (new Date()).getFullYear() }} <span class="d-none d-sm-inline-block">CookingCode.com</span>
    </div>
  </v-footer>
</template>

<script setup lang="ts">
  const version =
    (globalThis as any).__APP_VERSION__
    ?? (typeof __APP_VERSION__ !== 'undefined' ? __APP_VERSION__ : '0.0.0')
  const items = [
    {
      title: 'Vuetify Documentation',
      icon: `$vuetify`,
      href: 'https://vuetifyjs.com/',
    },
    {
      title: 'Vuetify Support',
      icon: 'mdi-shield-star-outline',
      href: 'https://support.vuetifyjs.com/',
    },
    {
      title: 'GitHub',
      icon: `mdi-github`,
      href: 'https://github.com/sinclapa/ccDiary',
    }
  ]
</script>

<style scoped lang="sass">
  .social-link :deep(.v-icon)
    color: rgba(var(--v-theme-on-background), var(--v-disabled-opacity))
    text-decoration: none
    transition: .2s ease-in-out

    &:hover
      color: rgba(25, 118, 210, 1)
</style>
