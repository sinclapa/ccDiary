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
  }>()

  type MapStatus = 'loading' | 'ready' | 'not-found' | 'error'

  const mapContainer = ref<HTMLElement | null>(null)
  const status = ref<MapStatus>('loading')
  let leafletMap: L.Map | null = null

  async function geocode (location: string): Promise<[number, number] | null> {
    const response = await fetch(
      `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(location)}&format=json&limit=1`
    )
    const results = await response.json()
    if (!results || results.length === 0) return null
    return [Number.parseFloat(results[0].lat), Number.parseFloat(results[0].lon)]
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

      status.value = 'ready'

      await nextTick()

      if (leafletMap) {
        leafletMap.remove()
        leafletMap = null
      }

      const bounds = L.latLngBounds([fromCoords, toCoords])
      leafletMap = L.map(mapContainer.value!).fitBounds(bounds, { padding: [40, 40] })
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(leafletMap)
      L.marker(fromCoords).addTo(leafletMap)
      L.marker(toCoords).addTo(leafletMap)
      L.polyline([fromCoords, toCoords], { color: 'red', dashArray: '6 4' }).addTo(leafletMap)
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
