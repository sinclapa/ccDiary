import { expect, test } from 'vitest'
import { mount } from '@vue/test-utils'
import LogoBrand from '@/components/LogoBrand.vue'

test('renders an SVG element', () => {
  const wrapper = mount(LogoBrand)
  expect(wrapper.find('svg').exists()).toBe(true)
})

test('SVG has correct viewBox', () => {
  const wrapper = mount(LogoBrand)
  expect(wrapper.find('svg').attributes('viewBox')).toBe('0 0 737 688')
})

test('SVG has aria-label for accessibility', () => {
  const wrapper = mount(LogoBrand)
  expect(wrapper.find('svg').attributes('aria-label')).toBe('CookingCode')
})

test('SVG has role="img"', () => {
  const wrapper = mount(LogoBrand)
  expect(wrapper.find('svg').attributes('role')).toBe('img')
})

test('all paths use currentColor fill for theme compatibility', () => {
  const wrapper = mount(LogoBrand)
  const paths = wrapper.findAll('path')
  expect(paths.length).toBeGreaterThan(0)
  for (const path of paths) {
    expect(path.attributes('fill')).toBe('currentColor')
  }
})
