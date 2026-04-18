import { describe, expect, it } from 'vitest'
import vuetify from '../vuetify'

describe('vuetify plugin', () => {
  it('exports a Vuetify instance', () => {
    expect(vuetify).toBeDefined()
  })

  it('registers all required mdi icon aliases', () => {
    const icons = (vuetify as any).icons
    const requiredAliases = [
      'mdi-account-circle',
      'mdi-api',
      'mdi-bird',
      'mdi-book-open-variant-outline',
      'mdi-car',
      'mdi-check-circle',
      'mdi-chevron-down',
      'mdi-chevron-up',
      'mdi-delete',
      'mdi-fast-forward',
      'mdi-ferry',
      'mdi-github',
      'mdi-image-plus',
      'mdi-login',
      'mdi-map-marker-off',
      'mdi-map-off',
      'mdi-pen',
      'mdi-pencil',
      'mdi-rewind',
      'mdi-server-network-off',
      'mdi-skip-backward',
      'mdi-skip-forward',
      'mdi-train',
      'mdi-walk',
    ]
    for (const alias of requiredAliases) {
      expect(icons.aliases[alias], `${alias} should be registered`).toBeTruthy()
    }
  })

  it('registers the swagger custom icon', () => {
    const icons = (vuetify as any).icons
    expect(icons.aliases.swagger).toBeDefined()
  })

  it('uses mdi as the default icon set', () => {
    const icons = (vuetify as any).icons
    expect(icons.defaultSet).toBe('mdi')
  })
})
