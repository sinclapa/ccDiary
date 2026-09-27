import L from 'leaflet'
import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
import markerIcon from 'leaflet/dist/images/marker-icon.png'
import markerShadow from 'leaflet/dist/images/marker-shadow.png'
import { computed, onUnmounted, ref } from 'vue'

// Vite rewrites asset URLs, which breaks Leaflet's own lookup of its default marker images;
// point it at the bundled ones. Done once, for every map in the app.
delete (L.Icon.Default.prototype as any)._getIconUrl
L.Icon.Default.mergeOptions({
  iconUrl: markerIcon,
  iconRetinaUrl: markerIcon2x,
  shadowUrl: markerShadow,
})

export type MapStatus = 'loading' | 'ready' | 'not-found' | 'error'

/**
 * What every Leaflet map in the app shares: its container, its loading status, an optional
 * height, keeping the map measured as its box changes, and removing it when the component goes.
 */
export function useLeafletMap (props: { height?: string }) {
  const mapContainer = ref<HTMLElement | null>(null)
  const status = ref<MapStatus>('loading')
  const heightStyle = computed(() => (props.height ? { height: props.height } : undefined))

  let map: L.Map | null = null
  let resizeObserver: ResizeObserver | null = null

  // Leaflet measures its container once. In a dialog that is still opening, or after the layout
  // around it changes, that size is stale and tiles draw into part of the box; re-measure.
  function watchSize () {
    resizeObserver?.disconnect()
    if (!mapContainer.value) return
    resizeObserver = new ResizeObserver(() => map?.invalidateSize?.())
    resizeObserver.observe(mapContainer.value)
  }

  /** Removes the current map, if any, before a new one is drawn or the component goes. */
  function clearMap () {
    resizeObserver?.disconnect()
    resizeObserver = null
    map?.remove()
    map = null
  }

  /** Adopts a newly created map and keeps it measured. */
  function setMap (created: L.Map) {
    map = created
    watchSize()
    return created
  }

  onUnmounted(clearMap)

  return { mapContainer, status, heightStyle, clearMap, setMap }
}
