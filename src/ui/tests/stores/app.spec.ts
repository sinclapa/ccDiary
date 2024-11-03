import { useAppStore } from '@/stores/app'
import { beforeEach, describe, expect, it, vi } from 'vitest'

describe('App Store', () => {
  it('Load', async () => {
    expect(useAppStore.$id).toMatch('app')
  })
})
