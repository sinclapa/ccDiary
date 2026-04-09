<template>
  <div class="journey-wrapper">
    <div v-if="status === 'loading'" class="journey-placeholder">
      <v-progress-circular color="red" indeterminate />
    </div>
    <div v-else-if="status === 'not-found'" class="journey-placeholder journey-not-found">
      <v-icon color="grey">mdi-map-marker-off</v-icon>
      <span class="text-caption text-grey">Location not found</span>
    </div>
    <div v-else-if="status === 'error'" class="journey-placeholder journey-error">
      <v-icon color="grey">mdi-map-off</v-icon>
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
    walking:      { color: '#2e7d32', weight: 2, dashArray: '2 6' },
    car:          { color: '#1565c0', weight: 3 },
    train:        { color: '#e65100', weight: 4, dashArray: '10 4' },
    boat:         { color: '#00838f', weight: 2, dashArray: '8 6' },
  }

  async function geocode (location: string): Promise<[number, number] | null> {
    const response = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(location)}&format=json&limit=1`
    )
    const results = await response.json()
    if (!results || results.length === 0) return null
    return [Number.parseFloat(results[0].lat), Number.parseFloat(results[0].lon)]
  }

  async function fetchOsrmRoute (
    from: [number, number],
    to: [number, number],
    profile: 'driving' | 'foot',
  ): Promise<[number, number][] | null> {
    try {
      const url = `https://router.project-osrm.org/route/v1/${profile}/${from[1]},${from[0]};${to[1]},${to[0]}?overview=full&geometries=geojson`
      const response = await fetch(url)
      const data = await response.json()
      if (data.code !== 'Ok' || !data.routes?.length) return null
      return (data.routes[0].geometry.coordinates as [number, number][]).map(([lon, lat]) => [lat, lon])
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
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(leafletMap)
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
  }

  .journey-container {
    width: 100%;
    height: 250px;
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
