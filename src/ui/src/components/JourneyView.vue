<template>
  <div class="journey-wrapper">
    <div v-if="status === 'loading'" class="journey-placeholder">
      <v-progress-circular color="red" indeterminate />
    </div>
    <div v-else-if="status === 'not-found'" class="journey-placeholder journey-not-found">
      <v-icon color="grey">$mdi-map-marker-off</v-icon>
      <span class="text-caption text-grey">Location not found</span>
    </div>
    <div v-else-if="status === 'error'" class="journey-placeholder journey-error">
      <v-icon color="grey">$mdi-map-off</v-icon>
      <span class="text-caption text-grey">Map unavailable</span>
    </div>
    <div
      ref="mapContainer"
      class="journey-container"
      :class="{ 'journey-hidden': status !== 'ready' }"
    />
  </div>
</template>

<script setup lang="ts">
  import 'leaflet/dist/leaflet.css'
  import L from 'leaflet'
  import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png'
  import markerIcon from 'leaflet/dist/images/marker-icon.png'
  import markerShadow from 'leaflet/dist/images/marker-shadow.png'
  import type { JourneyMode } from '@/services/models/diaryEntry'
  import { getAppConfigField } from '@/utils/appConfig'

  // Fix Vite asset URL resolution for Leaflet default marker icons
  delete (L.Icon.Default.prototype as any)._getIconUrl
  L.Icon.Default.mergeOptions({
    iconUrl: markerIcon,
    iconRetinaUrl: markerIcon2x,
    shadowUrl: markerShadow,
  })

  const props = defineProps<{
    fromLocation: string
    toLocation: string
    journeyMode?: JourneyMode
  }>()

  type MapStatus = 'loading' | 'ready' | 'not-found' | 'error'

  const mapContainer = ref<HTMLElement | null>(null)
  const status = ref<MapStatus>('loading')
  let leafletMap: L.Map | null = null

  const modeStyle: Record<NonNullable<JourneyMode>, { color: string; weight: number; dashArray?: string }> = {
    'crow-flies': { color: 'red', weight: 2, dashArray: '6 4' },
    walking: { color: '#2e7d32', weight: 2, dashArray: '2 6' },
    car: { color: '#1565c0', weight: 3 },
    train: { color: '#e65100', weight: 4, dashArray: '10 4' },
    boat: { color: '#00838f', weight: 2, dashArray: '8 6' },
  }

  const apiBase = getAppConfigField('VITE_API')

  async function geocode (location: string): Promise<[number, number] | null> {
    const response = await fetch(
      `${apiBase}v1/MapTile/Geocode?q=${encodeURIComponent(location)}`
    )
    if (!response.ok) return null
    const result = await response.json()
    return [result.lat as number, result.lon as number]
  }

  async function fetchOsrmRoute (
    from: [number, number],
    to: [number, number],
    profile: 'driving' | 'foot',
  ): Promise<[number, number][] | null> {
    try {
      const url = `${apiBase}v1/MapTile/Route?fromLat=${from[0]}&fromLon=${from[1]}&toLat=${to[0]}&toLon=${to[1]}&profile=${profile}`
      const response = await fetch(url)
      if (!response.ok) return null
      return await response.json() as [number, number][]
    } catch {
      return null
    }
  }

  async function buildRouteCoords (
    from: [number, number],
    to: [number, number],
  ): Promise<[number, number][]> {
    const mode = props.journeyMode ?? 'crow-flies'
    if (mode === 'walking') {
      const route = await fetchOsrmRoute(from, to, 'foot')
      if (route) return route
    } else if (mode === 'car') {
      const route = await fetchOsrmRoute(from, to, 'driving')
      if (route) return route
    }
    return [from, to]
  }

  async function initMap () {
    if (!props.fromLocation || !props.toLocation) {
      return
    }

    status.value = 'loading'

    try {
      const [fromCoords, toCoords] = await Promise.all([
        geocode(props.fromLocation),
        geocode(props.toLocation),
      ])

      if (!fromCoords || !toCoords) {
        status.value = 'not-found'
        return
      }

      const routeCoords = await buildRouteCoords(fromCoords, toCoords)

      status.value = 'ready'

      await nextTick()

      if (leafletMap) {
        leafletMap.remove()
        leafletMap = null
      }

      const style = modeStyle[props.journeyMode ?? 'crow-flies']
      const bounds = L.latLngBounds(routeCoords)
      leafletMap = L.map(mapContainer.value!).fitBounds(bounds, { padding: [40, 40] })
      L.tileLayer(`${apiBase}v1/MapTile/Tile/osm/{z}/{x}/{y}`, {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(leafletMap)
      if ((props.journeyMode ?? 'crow-flies') === 'boat') {
        L.tileLayer(`${apiBase}v1/MapTile/Tile/openseamap/{z}/{x}/{y}`, {
          attribution: '&copy; <a href="https://www.openseamap.org">OpenSeaMap</a> contributors',
          opacity: 0.8,
        }).addTo(leafletMap)
      }
      L.marker(fromCoords).addTo(leafletMap)
      L.marker(toCoords).addTo(leafletMap)
      L.polyline(routeCoords, style).addTo(leafletMap)
    } catch {
      status.value = 'error'
    }
  }

  onMounted(() => {
    initMap()
  })

  onUnmounted(() => {
    if (leafletMap) {
      leafletMap.remove()
      leafletMap = null
    }
  })

  watch(() => props.fromLocation, () => {
    initMap()
  })

  watch(() => props.toLocation, () => {
    initMap()
  })

  watch(() => props.journeyMode, () => {
    initMap()
  })
</script>

<style scoped>
  .journey-wrapper {
    width: 100%;
    isolation: isolate;
  }

  .journey-container {
    width: 100%;
    height: 250px;
    isolation: isolate;
  }

  .journey-hidden {
    display: none;
  }

  .journey-placeholder {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: 8px;
    height: 250px;
    background: rgb(var(--v-theme-surface-variant));
    border-radius: 4px;
  }
</style>
