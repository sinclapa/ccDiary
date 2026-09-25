import { describe, expect, it } from 'vitest'
import { mapInteraction } from '../mapInteraction'

const controls = ['dragging', 'touchZoom', 'scrollWheelZoom', 'doubleClickZoom', 'boxZoom', 'keyboard', 'zoomControl'] as const

describe('mapInteraction', () => {
  it('turns every pointer, wheel, touch and keyboard control off for a static map', () => {
    const options = mapInteraction(false)
    for (const control of controls) {
      expect(options[control], control).toBe(false)
    }
  })

  it('turns them all on for an interactive map', () => {
    const options = mapInteraction(true)
    for (const control of controls) {
      expect(options[control], control).toBe(true)
    }
  })
})
