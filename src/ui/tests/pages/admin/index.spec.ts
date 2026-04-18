import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'
import Component from '@/pages/admin/index.vue'
import { approveRequest, declineRequest, getPendingRequests } from '@/services/modules/adminService'
import type { AccessRequest } from '@/services/models/accessRequest'

vi.mock('@/services/modules/adminService')

globalThis.ResizeObserver = require('resize-observer-polyfill')

const mockRequest: AccessRequest = {
  accessRequestId: 'req-1',
  displayName: 'John Doe',
  email: 'john@example.com',
  status: 'pending',
  requestedAt: '2024-01-15T00:00:00Z',
}

describe('admin/index.vue', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    setActivePinia(createPinia())
    vi.clearAllMocks()
    vi.mocked(getPendingRequests).mockResolvedValue([mockRequest])
    vi.mocked(approveRequest).mockResolvedValue({ ok: true, redeemUrl: null })
    vi.mocked(declineRequest).mockResolvedValue(true)
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
  })

  afterEach(() => {
    wrapper.unmount()
  })

  it('renders the access requests heading', async () => {
    await flushPromises()
    expect(wrapper.text()).toContain('Access Requests')
  })

  it('calls getPendingRequests on mount', async () => {
    await flushPromises()
    expect(getPendingRequests).toHaveBeenCalled()
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
    vi.mocked(getPendingRequests).mockResolvedValue([])
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
})
