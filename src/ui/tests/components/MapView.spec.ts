import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import MapView from '@/components/MapView.vue'

const mockMapInstance = {
  setView: vi.fn().mockReturnThis(),
  remove: vi.fn(),
}

const mockTileLayerInstance = {
  addTo: vi.fn().mockReturnThis(),
}

const mockMarkerInstance = {
  addTo: vi.fn().mockReturnThis(),
}

vi.mock('leaflet', () => ({
  default: {
    map: vi.fn(() => mockMapInstance),
    tileLayer: vi.fn(() => mockTileLayerInstance),
    marker: vi.fn(() => mockMarkerInstance),
    Icon: {
      Default: {
        prototype: {},
        mergeOptions: vi.fn(),
      },
    },
  },
}))

const vuetify = createVuetify({ components, directives })

function mountMapView (location: string) {
  return mount(MapView, {
    props: { location },
    global: { plugins: [vuetify] },
  })
}

function stubFetchSuccess () {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: true,
    json: vi.fn().mockResolvedValue({ lat: 51.5074, lon: -0.1278 }),
  }))
}

function stubFetchNotFound () {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    ok: false,
    status: 404,
  }))
}

function stubFetchError () {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
}

describe('MapView.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders the map container div in the DOM', () => {
    stubFetchSuccess()
    const wrapper = mountMapView('London, UK')
    expect(wrapper.find('.map-container').exists()).toBe(true)
  })

  it('shows loading state before fetch resolves', () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() => new Promise(() => {})))
    const wrapper = mountMapView('London, UK')
    expect(wrapper.findComponent({ name: 'VProgressCircular' }).exists()).toBe(true)
    expect(wrapper.find('.map-container').classes()).toContain('map-hidden')
  })

  it('shows map and hides loading state when geocoding succeeds', async () => {
    stubFetchSuccess()
    const wrapper = mountMapView('London, UK')
    await flushPromises()
    expect(wrapper.findComponent({ name: 'VProgressCircular' }).exists()).toBe(false)
    expect(wrapper.find('.map-container').classes()).not.toContain('map-hidden')
  })

  it('calls L.map and L.marker when geocoding succeeds', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountMapView('London, UK')
    await flushPromises()
    expect(L.map).toHaveBeenCalledOnce()
    expect(L.marker).toHaveBeenCalledWith([51.5074, -0.1278])
  })

  it('uses the proxy tile URL for OSM tiles', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountMapView('London, UK')
    await flushPromises()
    const tileLayerCalls = (L.tileLayer as unknown as ReturnType<typeof vi.fn>).mock.calls
    expect(tileLayerCalls[0][0]).toContain('MapTile/Tile/osm/{z}/{x}/{y}')
    expect(tileLayerCalls[0][0]).not.toContain('openstreetmap.org')
  })

  it('uses the proxy geocode URL, not Nominatim directly', async () => {
    stubFetchSuccess()
    mountMapView('London, UK')
    await flushPromises()
    const fetchMock = fetch as unknown as ReturnType<typeof vi.fn>
    const geocodeCall = fetchMock.mock.calls[0][0] as string
    expect(geocodeCall).toContain('MapTile/Geocode')
    expect(geocodeCall).not.toContain('nominatim.openstreetmap.org')
  })

  it('shows not-found state when geocoding returns 404', async () => {
    stubFetchNotFound()
    const wrapper = mountMapView('zzz_nonexistent_place_xyz')
    await flushPromises()
    expect(wrapper.find('.map-not-found').exists()).toBe(true)
    expect(wrapper.text()).toContain('Location not found')
  })

  it('shows error state when fetch throws', async () => {
    stubFetchError()
    const wrapper = mountMapView('London, UK')
    await flushPromises()
    expect(wrapper.find('.map-error').exists()).toBe(true)
    expect(wrapper.text()).toContain('Map unavailable')
  })

  it('does not call fetch when location is empty string', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    mountMapView('')
    await flushPromises()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('calls leafletMap.remove() when component is unmounted', async () => {
    stubFetchSuccess()
    const wrapper = mountMapView('London, UK')
    await flushPromises()
    wrapper.unmount()
    expect(mockMapInstance.remove).toHaveBeenCalled()
  })

  it('re-geocodes and re-creates map when location prop changes', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    const wrapper = mountMapView('Paris, France')
    await flushPromises()
    expect(L.map).toHaveBeenCalledTimes(1)

    await wrapper.setProps({ location: 'Berlin, Germany' })
    await flushPromises()
    expect(fetch).toHaveBeenCalledTimes(2)
    expect(L.map).toHaveBeenCalledTimes(2)
  })

  it('cleans up existing map before creating a new one on location change', async () => {
    stubFetchSuccess()
    const wrapper = mountMapView('Paris, France')
    await flushPromises()

    await wrapper.setProps({ location: 'Berlin, Germany' })
    await flushPromises()
    expect(mockMapInstance.remove).toHaveBeenCalledTimes(1)
  })
})
