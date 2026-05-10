<template>
  <v-footer app class="app-footer py-0" height="auto">
    <div class="d-flex flex-column w-100">
      <div class="d-flex align-center w-100 footer-row">
        <div class="text-caption text-disabled">
          <span>{{ environment ? `${environment} ` : '' }}Version {{ version }}</span>
        </div>
        <a
          class="d-inline-block mx-2 social-link brand-link"
          href="https://cookingcode.com"
          rel="noopener noreferrer"
          target="_blank"
          title="CookingCode"
        >
          <img :src="brandLogo" alt="CookingCode" height="16" />
        </a>

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
            :size="16"
          />
        </a>

        <div class="text-caption text-disabled ms-auto">
          &copy; 2023-{{ (new Date()).getFullYear() }} <span class="d-none d-sm-inline-block">CookingCode.com</span>
        </div>
      </div>

      <div class="d-flex align-center justify-center w-100 footer-row--secondary">
        <button class="text-caption text-disabled cookie-pref-btn" @click="openPreferences">
          Cookie preferences
        </button>
      </div>
    </div>
  </v-footer>
</template>

<script setup lang="ts">
  import { computed } from 'vue'
  import { useTheme } from 'vuetify'
  import { getAppConfigField } from '@/utils/appConfig'
  import { useConsent } from '@/composables/useConsent'
  import logoSimpleLight from '@/assets/logo-simple-light.svg'
  import logoSimpleDark from '@/assets/logo-simple-dark.svg'

  const theme = useTheme()
  const brandLogo = computed(() => theme.global.name.value === 'dark' ? logoSimpleDark : logoSimpleLight)

  const version = __APP_VERSION__
  const environment = getAppConfigField('VITE_ENVIRONMENT', { defaultValue: '' })
  const apiUrl = getAppConfigField('VITE_API', { defaultValue: '' })
  const items = [
    {
      title: 'GitHub',
      icon: '$mdi-github',
      href: 'https://github.com/sinclapa/ccDiary',
    },
    {
      title: 'Swagger API',
      icon: '$swagger',
      href: apiUrl ? new URL('/swagger', apiUrl).href : '',
    },
    {
      title: `API ${apiUrl}`,
      icon: '$mdi-api',
      href: apiUrl ? new URL('/', apiUrl).href : '',
    },
  ]

  const { openPreferences } = useConsent()
</script>

<style scoped lang="sass">
  .app-footer
    position: fixed !important
    bottom: 0
    left: 0
    right: 0
    z-index: 1004
    border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity))

  .footer-row
    min-height: 28px

  .footer-row--secondary
    min-height: 14px
    padding-bottom: 3px

  .social-link :deep(.v-icon)
    color: rgba(var(--v-theme-on-background), var(--v-disabled-opacity))
    text-decoration: none
    transition: .2s ease-in-out

    &:hover
      color: rgba(25, 118, 210, 1)

  .brand-link
    display: inline-flex !important
    align-items: center
    opacity: var(--v-disabled-opacity)
    text-decoration: none
    color: rgba(var(--v-theme-on-background), 1)
    transition: .2s ease-in-out

    &:hover
      opacity: 1

  .cookie-pref-btn
    background: none
    border: none
    padding: 0
    cursor: pointer
    color: inherit
    font: inherit
    text-decoration: underline
</style>
