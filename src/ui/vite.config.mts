// Plugins
/// <reference types="vitest" />
import AutoImport from 'unplugin-auto-import/vite'
import Components from 'unplugin-vue-components/vite'
import Fonts from 'unplugin-fonts/vite'
import Layouts from 'vite-plugin-vue-layouts'
import Vue from '@vitejs/plugin-vue'
import VueRouter from 'unplugin-vue-router/vite'
import Vuetify, { transformAssetUrls } from 'vite-plugin-vuetify'

// Utilities
import { defineConfig } from 'vite'
import path from 'node:path'
import pkg from './package.json'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    VueRouter({
      dts: 'src/typed-router.d.ts',
    }),
    Layouts(),
    AutoImport({
      imports: [
        'vue',
        {
          'vue-router/auto': ['useRoute', 'useRouter'],
        },
      ],
      dts: 'src/auto-imports.d.ts',
      eslintrc: {
        enabled: true,
      },
      vueTemplate: true,
    }),
    Components({
      dts: 'src/components.d.ts',
    }),
    Vue({
      template: { transformAssetUrls },
    }),
    // https://github.com/vuetifyjs/vuetify-loader/tree/master/packages/vite-plugin#readme
    Vuetify({
      autoImport: true,
      styles: {
        configFile: 'src/styles/settings.scss',
      },
    }),
    Fonts({
      google: {
        families: [{
          name: 'Roboto',
          styles: 'wght@100;300;400;500;700;900',
        }],
      },
    }),
  ],
  define: {
    'process.env': {},
    __APP_VERSION__: JSON.stringify(pkg.version),
  },
  resolve: {
    alias: {
      // '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@': path.resolve(__dirname, 'src'),
    },
    extensions: [
      '.js',
      '.json',
      '.jsx',
      '.mjs',
      '.ts',
      '.tsx',
      '.vue',
    ],
  },
  server: {
    port: 8080,
    strictPort: true,
  },
  test: {
    unstubEnvs: true,
    globals: true,
    environment: 'happy-dom',
    exclude: ['**/node_modules/**', '**/dist/**', 'e2e/**'],
    server: {
      deps: {
        inline: ['vuetify', 'leaflet'],
      },
    },
    coverage: {
      reporter: ['lcov', 'cobertura', 'text', 'html'],
      provider: 'v8',
      exclude: [
        'src/plugins/index.ts',
        'src/plugins/vuetify.ts',
        'src/router/index.ts',
        'src/auto-imports.d.ts',
        'src/components.d.ts',
        'src/env.d.ts',
        'src/main.ts',
        'src/typed-router.d.ts',
        'src/vite-env.d.ts',
        'src/stores/index.ts',
        '.eslintrc.js',
        'vite.config.mts',
        '**/dist/**',
        'e2e/**',
        'playwright.config.ts',
        'public/**',
      ],
    },
    setupFiles: './tests/setupTests.ts',
  },
})
