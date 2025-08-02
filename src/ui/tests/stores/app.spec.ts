import { setActivePinia, createPinia } from 'pinia'
import { describe, it, expect, beforeEach } from 'vitest'
import { useAppStore } from '@/stores/app'

describe('useAppStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('can be instantiated', () => {
    const store = useAppStore()
    expect(store).toBeDefined()
    expect(typeof store).toBe('object')
  })

  it('has default state', () => {
    const store = useAppStore()
    expect(store.$state).toEqual({})
  })
})
