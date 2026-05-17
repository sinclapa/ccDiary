import { afterEach, beforeEach, describe, expect, it, type MockInstance, vi } from 'vitest'
import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'
import Component from '@/pages/admin/index.vue'
import { approveRequest, declineRequest, deleteRequest, getAllRequests, resendInvitation } from '@/services/modules/adminService'
import type { AccessRequest } from '@/services/models/accessRequest'

vi.mock('@/services/modules/adminService')

globalThis.ResizeObserver = require('resize-observer-polyfill')

const mockApprovedRequest: AccessRequest = {
  accessRequestId: 'req-2',
  displayName: 'Jane Smith',
  email: 'jane@example.com',
  status: 'approved',
  requestedAt: '2024-01-10T00:00:00Z',
  inviteRedeemUrl: 'https://ms.example/invite/abc',
}

const mockRequest: AccessRequest = {
  accessRequestId: 'req-1',
  displayName: 'John Doe',
  email: 'john@example.com',
  status: 'pending',
  requestedAt: '2024-01-15T00:00:00Z',
}

describe('admin/index.vue', () => {
  let wrapper: VueWrapper
  let clipboardSpy: MockInstance

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    clipboardSpy = vi.fn()
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText: clipboardSpy },
      configurable: true,
    })
    vi.mocked(getAllRequests).mockResolvedValue([mockRequest])
    vi.mocked(approveRequest).mockResolvedValue({ ok: true, redeemUrl: null })
    vi.mocked(declineRequest).mockResolvedValue(true)
    vi.mocked(deleteRequest).mockResolvedValue(true)
    vi.mocked(resendInvitation).mockResolvedValue({ ok: true, redeemUrl: null })
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
  })

  afterEach(() => {
    wrapper.unmount()
  })

  it('renders the access requests heading', async () => {
    await flushPromises()
    expect(wrapper.text()).toContain('Access Requests')
  })

  it('calls getAllRequests on mount', async () => {
    await flushPromises()
    expect(getAllRequests).toHaveBeenCalled()
  })

  it('displays request display name in the table', async () => {
    await flushPromises()
    expect(wrapper.text()).toContain('John Doe')
  })

  it('displays request email in the table', async () => {
    await flushPromises()
    expect(wrapper.text()).toContain('john@example.com')
  })

  it('shows no data text when no requests are returned', async () => {
    vi.mocked(getAllRequests).mockResolvedValue([])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    expect(wrapper.text()).toContain('No pending access requests')
  })

  it('calls approveRequest when approve button is clicked', async () => {
    await flushPromises()
    const approveBtn = wrapper.find('#req-1_approve')
    expect(approveBtn.exists()).toBe(true)
    await approveBtn.trigger('click')
    await flushPromises()
    expect(approveRequest).toHaveBeenCalledWith('req-1')
  })

  it('shows success feedback and reloads after approve', async () => {
    await flushPromises()
    await wrapper.find('#req-1_approve').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('John Doe')
    expect(wrapper.text()).toContain('approved')
  })

  it('shows redeem URL in feedback when approve returns one', async () => {
    vi.mocked(approveRequest).mockResolvedValue({ ok: true, redeemUrl: 'https://ms.example/invite/xyz' })
    await flushPromises()
    await wrapper.find('#req-1_approve').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('https://ms.example/invite/xyz')
  })

  it('shows error feedback when approve fails', async () => {
    vi.mocked(approveRequest).mockResolvedValue({ ok: false, redeemUrl: null })
    await flushPromises()
    await wrapper.find('#req-1_approve').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Failed to approve')
  })

  it('calls declineRequest when decline button is clicked', async () => {
    await flushPromises()
    const declineBtn = wrapper.find('#req-1_decline')
    expect(declineBtn.exists()).toBe(true)
    await declineBtn.trigger('click')
    await flushPromises()
    expect(declineRequest).toHaveBeenCalledWith('req-1')
  })

  it('shows success feedback and reloads after decline', async () => {
    await flushPromises()
    await wrapper.find('#req-1_decline').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('declined')
  })

  it('shows error feedback when decline fails', async () => {
    vi.mocked(declineRequest).mockResolvedValue(false)
    await flushPromises()
    await wrapper.find('#req-1_decline').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Failed to decline')
  })

  it('clears feedback when close is clicked on the alert', async () => {
    await flushPromises()
    await wrapper.find('#req-1_approve').trigger('click')
    await flushPromises()
    expect((wrapper.vm as any).feedbackMessage).toBeTruthy()
    ;(wrapper.vm as any).clearFeedback()
    await flushPromises()
    expect((wrapper.vm as any).feedbackMessage).toBe('')
    expect((wrapper.vm as any).redeemUrl).toBeNull()
  })

  it('clears feedback when the tab changes', async () => {
    await flushPromises()
    await wrapper.find('#req-1_approve').trigger('click')
    await flushPromises()
    expect((wrapper.vm as any).feedbackMessage).toBeTruthy()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    expect((wrapper.vm as any).feedbackMessage).toBe('')
    expect((wrapper.vm as any).redeemUrl).toBeNull()
  })

  it('shows copy button for approved requests', async () => {
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    expect(wrapper.find('#req-2_copy').exists()).toBe(true)
  })

  it('calls deleteRequest when delete button is clicked on an approved entry', async () => {
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_delete').trigger('click')
    await flushPromises()
    expect(deleteRequest).toHaveBeenCalledWith('req-2')
  })

  it('shows success feedback after deleting an approved entry', async () => {
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_delete').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('deleted')
  })

  it('shows error feedback when delete fails', async () => {
    vi.mocked(deleteRequest).mockResolvedValue(false)
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_delete').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Failed to delete')
  })

  it('copies the invite link to the clipboard when the copy button is clicked', async () => {
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_copy').trigger('click')
    expect(clipboardSpy).toHaveBeenCalledWith('https://ms.example/invite/abc')
  })

  it('calls resendInvitation when resend button is clicked on an approved entry', async () => {
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_resend').trigger('click')
    await flushPromises()
    expect(resendInvitation).toHaveBeenCalledWith('req-2')
  })

  it('shows success feedback after resend', async () => {
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_resend').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('resent')
  })

  it('shows redeem URL in feedback when resend returns one', async () => {
    vi.mocked(resendInvitation).mockResolvedValue({ ok: true, redeemUrl: 'https://ms.example/invite/xyz' })
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_resend').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('https://ms.example/invite/xyz')
  })

  it('shows error feedback when resend fails', async () => {
    vi.mocked(resendInvitation).mockResolvedValue({ ok: false, redeemUrl: null })
    vi.mocked(getAllRequests).mockResolvedValue([mockApprovedRequest])
    wrapper.unmount()
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
    await flushPromises()
    ;(wrapper.vm as any).tab = 'approved'
    await flushPromises()
    await wrapper.find('#req-2_resend').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Failed to resend')
  })
})
