import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import { getMe } from '@/services/modules/userService'

vi.mock('@/services/modules/userService')

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
  })

  it('has null appUser by default', () => {
    const store = useAuthStore()
    expect(store.appUser).toBeNull()
  })

  it('fetchAppUser sets appUser from getMe', async () => {
    const user = { userId: 'u1', displayName: 'Admin', email: 'a@b.com', role: 'diary-admin' as const, entraObjectId: 'oid' }
    vi.mocked(getMe).mockResolvedValue(user)

    const store = useAuthStore()
    await store.fetchAppUser()

    expect(store.appUser).toEqual(user)
  })

  it('clearAppUser sets appUser to null', async () => {
    const user = { userId: 'u1', displayName: 'Admin', email: 'a@b.com', role: 'diary-admin' as const, entraObjectId: 'oid' }
    vi.mocked(getMe).mockResolvedValue(user)

    const store = useAuthStore()
    await store.fetchAppUser()
    store.clearAppUser()

    expect(store.appUser).toBeNull()
  })

  it('isAdmin is true when role is diary-admin', async () => {
    const user = { userId: 'u1', displayName: 'Admin', email: 'a@b.com', role: 'diary-admin' as const, entraObjectId: 'oid' }
    vi.mocked(getMe).mockResolvedValue(user)

    const store = useAuthStore()
    await store.fetchAppUser()

    expect(store.isAdmin).toBe(true)
    expect(store.isContributor).toBe(true)
  })

  it('isAdmin is false and isContributor is true for diary-contributor', async () => {
    const user = { userId: 'u2', displayName: 'User', email: 'u@b.com', role: 'diary-contributor' as const, entraObjectId: 'oid2' }
    vi.mocked(getMe).mockResolvedValue(user)

    const store = useAuthStore()
    await store.fetchAppUser()

    expect(store.isAdmin).toBe(false)
    expect(store.isContributor).toBe(true)
  })

  it('isAdmin and isContributor are false when appUser is null', () => {
    const store = useAuthStore()

    expect(store.isAdmin).toBe(false)
    expect(store.isContributor).toBe(false)
  })
})
