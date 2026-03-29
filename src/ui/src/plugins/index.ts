/**
 * plugins/index.ts
 *
 * Automatically included in `./src/main.ts`
 */

// Plugins
import vuetify from './vuetify'
import pinia from '../stores'
import router from '../router'
import { initFaro } from './faro'

// Types
import type { App } from 'vue'

export function registerPlugins (app: App) {
  initFaro()
  app
    .use(vuetify)
    .use(router)
    .use(pinia)
}
