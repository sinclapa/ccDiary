import type { MapOptions } from 'leaflet'

/**
 * Leaflet options for a map that either responds to the pointer or sits still.
 *
 * A timeline map is inline with the page, so dragging, wheel and pinch zoom there capture the
 * scroll meant for the page. Those maps are static and open full size on click, where the same
 * map is interactive.
 */
export function mapInteraction (interactive: boolean): MapOptions {
  return {
    dragging: interactive,
    touchZoom: interactive,
    scrollWheelZoom: interactive,
    doubleClickZoom: interactive,
    boxZoom: interactive,
    keyboard: interactive,
    zoomControl: interactive,
  }
}
