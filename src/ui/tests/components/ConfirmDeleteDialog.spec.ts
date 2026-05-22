import { mount } from '@vue/test-utils'
import { afterEach, describe, expect, it } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import ConfirmDeleteDialog from '@/components/ConfirmDeleteDialog.vue'

const vuetify = createVuetify({ components, directives })

globalThis.ResizeObserver = require('resize-observer-polyfill')

const wrappers: ReturnType<typeof mount>[] = []

function mountDialog (overrides: Partial<InstanceType<typeof ConfirmDeleteDialog>['$props']> = {}) {
  const wrapper = mount(ConfirmDeleteDialog, {
    attachTo: document.body,
    props: {
      modelValue: true,
      title: 'Delete Item',
      itemType: 'item',
      confirmLabel: 'Delete Item',
      items: [
        { label: 'Name', value: 'Test Item' },
        { label: 'Owner', value: 'Test User' },
      ],
      ...overrides,
    },
    global: { plugins: [vuetify] },
  })
  wrappers.push(wrapper)
  return wrapper
}

describe('ConfirmDeleteDialog.vue', () => {
  afterEach(() => {
    wrappers.forEach(w => w.unmount())
    wrappers.length = 0
  })

  it('renders the title', () => {
    mountDialog()
    expect(document.body.textContent).toContain('Delete Item')
  })

  it('renders all metadata rows', () => {
    mountDialog()
    expect(document.body.textContent).toContain('Name:')
    expect(document.body.textContent).toContain('Test Item')
    expect(document.body.textContent).toContain('Owner:')
    expect(document.body.textContent).toContain('Test User')
  })

  it('renders the item type in the confirmation message', () => {
    mountDialog({ itemType: 'diary' })
    expect(document.body.textContent).toContain('permanently delete this diary')
  })

  it('renders the confirm button label', () => {
    mountDialog({ confirmLabel: 'Delete Diary' })
    expect(document.body.textContent).toContain('Delete Diary')
  })

  it('emits confirm when confirm button is clicked', async () => {
    const wrapper = mountDialog()
    const buttons = wrapper.findAllComponents({ name: 'VBtn' })
    const confirmBtn = buttons.find(b => b.text() === 'Delete Item')
    expect(confirmBtn).toBeTruthy()
    await confirmBtn!.trigger('click')
    expect(wrapper.emitted('confirm')).toHaveLength(1)
  })

  it('emits cancel when Cancel button is clicked', async () => {
    const wrapper = mountDialog()
    const buttons = wrapper.findAllComponents({ name: 'VBtn' })
    const cancelBtn = buttons.find(b => b.text() === 'Cancel')
    expect(cancelBtn).toBeTruthy()
    await cancelBtn!.trigger('click')
    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('renders with multiple metadata items', () => {
    mountDialog({
      items: [
        { label: 'Date', value: 'Mon 1 Jan 2024' },
        { label: 'Time', value: '12:00' },
        { label: 'Location', value: 'London' },
      ],
    })
    expect(document.body.textContent).toContain('Date:')
    expect(document.body.textContent).toContain('Mon 1 Jan 2024')
    expect(document.body.textContent).toContain('Time:')
    expect(document.body.textContent).toContain('12:00')
    expect(document.body.textContent).toContain('Location:')
    expect(document.body.textContent).toContain('London')
  })
})
