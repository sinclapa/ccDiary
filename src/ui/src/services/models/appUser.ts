export type AppUserRole = 'diary-admin' | 'diary-contributor'

export interface AppUser {
  userId: string
  displayName: string
  email: string
  role: AppUserRole
  entraObjectId: string
}
