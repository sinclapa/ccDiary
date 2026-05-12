import { mount, VueWrapper } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Component from '@/pages/index.vue'

const vuetify = createVuetify({
  components,
  directives,
})

describe('pages/Index.vue', () => {
  let wrapper: VueWrapper

  beforeEach(() => {
    wrapper = mount(Component, {
      global: {
        plugins: [vuetify],
      },
    })
  })

  afterEach(() => {
    wrapper.unmount()
  })

  it('renders the hero title with split styling', () => {
    const heroTitle = wrapper.find('.hero-title')
    expect(heroTitle.exists()).toBe(true)
  })

  it('renders Coooking in cc style', () => {
    const ccSpan = wrapper.find('.hero-title__cc')
    expect(ccSpan.exists()).toBe(true)
    expect(ccSpan.text()).toBe('Cooking')
  })

  it('renders Code in diary style', () => {
    const diarySpans = wrapper.findAll('.hero-title__diary')
    // Should have two diary-styled spans: one for "Code" and one for "Diary"
    expect(diarySpans.length).toBeGreaterThanOrEqual(1)
    const codeSpan = diarySpans[0]
    expect(codeSpan.text()).toBe('Code')
  })

  it('renders Diary on second line in diary style', () => {
    const diaryLine = wrapper.find('.hero-title__line--second')
    expect(diaryLine.exists()).toBe(true)
    expect(diaryLine.text()).toBe('Diary')
  })

  it('renders the welcome text', () => {
    expect(wrapper.text()).toContain('Welcome to the Cooking Code Diary App')
  })

  it('renders the logo image', () => {
    expect(wrapper.findComponent({ name: 'VImg' }).exists()).toBe(true)
  })
})
