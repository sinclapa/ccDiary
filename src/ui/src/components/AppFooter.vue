<template>
  <v-footer app height="40" class="app-footer">
    <div class="text-caption text-disabled">
      <span>{{ environment ? `${environment} ` : '' }}Version {{ version }}</span>
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
        :size=16
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
  import { getAppConfigField } from '@/utils/appConfig'

  const version = __APP_VERSION__
  const environment = getAppConfigField('VITE_ENVIRONMENT', { defaultValue: '' })
  const apiUrl = getAppConfigField('VITE_API', { defaultValue: '' })
  const items = [
    {
      title: 'Vuetify Documentation',
      icon: `$vuetify`,
      href: 'https://vuetifyjs.com/',
    },
    {
      title: 'GitHub',
      icon: `mdi-github`,
      href: 'https://github.com/sinclapa/ccDiary',
    },
    {
      title: 'Swagger API',
      icon: '$swagger',
      href: apiUrl ? new URL('/swagger', apiUrl).href : '',
    },
    {
      title: `API ${apiUrl}`,
      icon: 'mdi-api',
      href: apiUrl ? new URL('/', apiUrl).href : '',
    },
  ]
</script>

<style scoped lang="sass">
  .app-footer
    position: fixed !important
    bottom: 0
    left: 0
    right: 0
    z-index: 1004

  .social-link :deep(.v-icon)
    color: rgba(var(--v-theme-on-background), var(--v-disabled-opacity))
    text-decoration: none
    transition: .2s ease-in-out

    &:hover
      color: rgba(25, 118, 210, 1)
</style>
