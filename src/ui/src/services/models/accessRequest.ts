export interface AccessRequest {
  accessRequestId: string
  displayName: string
  email: string
  status: 'pending' | 'approved' | 'declined'
  requestedAt: string
  processedAt?: string | null
  inviteRedeemUrl?: string | null
}
