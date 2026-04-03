import { beforeEach, describe, expect, it } from 'vitest'
import { getAppConfigField } from '../appConfig'

describe('getAppConfigField', () => {
  beforeEach(() => {
    // reset global window config and import.meta.env between tests
    (globalThis as any).APP_CONFIG = undefined
    ;(import.meta as any).env = {}
  })

  it('returns value from window.APP_CONFIG when present and not placeholder', () => {
    (globalThis as any).APP_CONFIG = { TEST_KEY: 'windowValue' }
    const v = getAppConfigField('TEST_KEY')
    expect(v).toBe('windowValue')
  })

  it('returns defaultValue when neither window.APP_CONFIG nor import.meta.env provides a value', () => {
    const v = getAppConfigField('MISSING_KEY', { defaultValue: 'DEFAULT' })
    expect(v).toBe('DEFAULT')
  })
})
