import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, VueWrapper } from '@vue/test-utils'
import { nextTick } from 'vue'
import vuetify from '@/../tests/plugins/vuetify-test-plugin'
import Component from '@/pages/register.vue'
import { submitAccessRequest } from '@/services/modules/accessRequestService'
import { state } from '@/services/authentication/msalConfig'

const mockRouterReplace = vi.fn()

vi.mock('vue-router', () => ({
  useRouter: () => ({
    replace: mockRouterReplace,
    currentRoute: { value: { path: '/register' } },
  }),
}))

vi.mock('@/services/modules/accessRequestService')

describe('register.vue', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    vi.clearAllMocks()
    state.isAuthenticated = false
    vi.mocked(submitAccessRequest).mockResolvedValue(undefined)
    wrapper = mount(Component, { global: { plugins: [vuetify] } })
  })

  afterEach(() => {
    state.isAuthenticated = false
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

  describe('redirect — authenticated users', () => {
    it('redirects to / immediately when already authenticated on mount', () => {
      wrapper.unmount()
      state.isAuthenticated = true
      mount(Component, { global: { plugins: [vuetify] } })
      expect(mockRouterReplace).toHaveBeenCalledWith('/')
    })

    it('redirects to / when state.isAuthenticated changes to true after mount', async () => {
      expect(mockRouterReplace).not.toHaveBeenCalled()
      state.isAuthenticated = true
      await nextTick()
      expect(mockRouterReplace).toHaveBeenCalledWith('/')
    })

    it('does not redirect when state.isAuthenticated stays false', async () => {
      await nextTick()
      expect(mockRouterReplace).not.toHaveBeenCalled()
    })
  })
})
