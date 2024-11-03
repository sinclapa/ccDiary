import { useAppStore } from '@/stores/app'
import { describe, expect, it } from 'vitest'

describe('App Store', () => {
  it('Load', async () => {
    expect(useAppStore.$id).toMatch('app')
  })
})
