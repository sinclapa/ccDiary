import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'
import Component from '@/pages/register.vue'
import { submitAccessRequest } from '@/services/modules/accessRequestService'

vi.mock('@/services/modules/accessRequestService')

describe('register.vue', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    vi.clearAllMocks()
    vi.mocked(submitAccessRequest).mockResolvedValue(undefined)
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
  })

  afterEach(() => {
    wrapper.unmount()
  })

  it('renders the request access form initially', () => {
    expect(wrapper.text()).toContain('Request Access')
    expect(wrapper.find('form').exists()).toBe(true)
  })

  it('shows submitted state after successful submission', async () => {
    const vm = wrapper.vm as any
    // Set field values directly on refs
    vm.displayName = 'John Doe'
    vm.email = 'john@example.com'
    // Mock form validate to return valid
    vm.form = { validate: vi.fn().mockResolvedValue({ valid: true }) }
    await vm.submit()
    await flushPromises()
    expect(submitAccessRequest).toHaveBeenCalledWith('John Doe', 'john@example.com')
    expect(wrapper.text()).toContain('Request Submitted')
  })

  it('does not submit if form is invalid', async () => {
    const vm = wrapper.vm as any
    vm.form = { validate: vi.fn().mockResolvedValue({ valid: false }) }
    await vm.submit()
    await flushPromises()
    expect(submitAccessRequest).not.toHaveBeenCalled()
  })

  it('shows error message when submission throws', async () => {
    const vm = wrapper.vm as any
    vm.displayName = 'Jane'
    vm.email = 'jane@example.com'
    vm.form = { validate: vi.fn().mockResolvedValue({ valid: true }) }
    vi.mocked(submitAccessRequest).mockRejectedValue(new Error('Server error'))
    await vm.submit()
    await flushPromises()
    expect(vm.error).toBe('Server error')
  })

  it('shows generic error when thrown value is not an Error', async () => {
    const vm = wrapper.vm as any
    vm.displayName = 'Jane'
    vm.email = 'jane@example.com'
    vm.form = { validate: vi.fn().mockResolvedValue({ valid: true }) }
    vi.mocked(submitAccessRequest).mockRejectedValue('unexpected')
    await vm.submit()
    await flushPromises()
    expect(vm.error).toBe('An error occurred. Please try again.')
  })
})
