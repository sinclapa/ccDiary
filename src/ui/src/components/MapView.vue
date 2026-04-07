<template>
  <div class="map-wrapper">
    <div v-if="status === 'loading'" class="map-placeholder">
      <v-progress-circular color="red" indeterminate />
    </div>
    <div v-else-if="status === 'not-found'" class="map-placeholder map-not-found">
      <v-icon color="grey">mdi-map-marker-off</v-icon>
      <span class="text-caption text-grey">Location not found</span>
    </div>
    <div v-else-if="status === 'error'" class="map-placeholder map-error">
      <v-icon color="grey">mdi-map-off</v-icon>
      <span class="text-caption text-grey">Map unavailable</span>
    </div>
    <div
      ref="mapContainer"
      class="map-container"
      :class="{ 'map-hidden': status !== 'ready' }"
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
    location: string
  }>()

  type MapStatus = 'loading' | 'ready' | 'not-found' | 'error'

  const mapContainer = ref<HTMLElement | null>(null)
  const status = ref<MapStatus>('loading')
  let leafletMap: L.Map | null = null

  async function initMap () {
    if (!props.location) {
      return
    }

    status.value = 'loading'

    try {
      const response = await fetch(
        `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(props.location)}&format=json&limit=1`
      )
      const results = await response.json()

      if (!results || results.length === 0) {
        status.value = 'not-found'
        return
      }

      const { lat, lon } = results[0]
      status.value = 'ready'

      // Wait for Vue to remove the map-hidden class before Leaflet measures dimensions
      await nextTick()

      if (leafletMap) {
        leafletMap.remove()
        leafletMap = null
      }

      leafletMap = L.map(mapContainer.value!).setView([Number.parseFloat(lat), Number.parseFloat(lon)], 13)
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }).addTo(leafletMap)
      L.marker([Number.parseFloat(lat), Number.parseFloat(lon)]).addTo(leafletMap)
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

  watch(() => props.location, () => {
    initMap()
  })
</script>

<style scoped>
  .map-wrapper {
    width: 100%;
  }

  .map-container {
    width: 100%;
    height: 250px;
  }

  .map-hidden {
    display: none;
  }

  .map-placeholder {
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
