<template>
  <footer class="app-footer py-0">
    <div class="d-flex flex-column footer-inner">
      <div class="d-flex align-center justify-center w-100 footer-row">
        <a
          class="d-inline-block mx-2 social-link brand-link"
          href="https://cookingcode.com"
          rel="noopener noreferrer"
          target="_blank"
          title="CookingCode"
        >
          <img alt="CookingCode" height="16" :src="brandLogo">
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
      </div>

      <div class="d-flex align-center justify-center w-100 footer-row">
        <div class="text-body-2 text-disabled text-center">
          &copy; {{ (new Date()).getFullYear() }} Cooking Code. Designed by
          <a class="footer-copy-link" href="https://en.wikipedia.org/wiki/Human" rel="noopener noreferrer" target="_blank">Humans</a>
          built by
          <a class="footer-copy-link" href="https://claude.ai/login" rel="noopener noreferrer" target="_blank">Claude</a>
        </div>
      </div>

      <div class="d-flex align-center justify-center w-100 footer-row--secondary">
        <button class="text-caption text-disabled cookie-pref-btn" @click="openPreferences">
          Cookie preferences
        </button>
      </div>
    </div>
  </footer>
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
    border-top: 1px solid rgba(var(--v-border-color), var(--v-border-opacity))

  .footer-inner
    max-width: 1100px
    width: 100%
    margin: 0 auto

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
      color: rgb(var(--v-theme-primary))

  .brand-link
    display: inline-flex !important
    align-items: center
    opacity: var(--v-disabled-opacity)
    text-decoration: none
    color: rgba(var(--v-theme-on-background), 1)
    transition: .2s ease-in-out

    &:hover
      opacity: 1

  .footer-copy-link
    color: rgb(var(--v-theme-primary))
    text-decoration: none
    transition: .2s ease-in-out

    &:hover
      text-decoration: underline

  .cookie-pref-btn
    background: none
    border: none
    padding: 0
    cursor: pointer
    color: inherit
    font: inherit
    text-decoration: underline
</style>
