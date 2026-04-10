import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import JourneyView from '@/components/JourneyView.vue'
import type { JourneyMode } from '@/services/models/diaryEntry'

const mockMapInstance = {
  setView: vi.fn().mockReturnThis(),
  fitBounds: vi.fn().mockReturnThis(),
  remove: vi.fn(),
}

const mockTileLayerInstance = {
  addTo: vi.fn().mockReturnThis(),
}

const mockMarkerInstance = {
  addTo: vi.fn().mockReturnThis(),
}

const mockPolylineInstance = {
  addTo: vi.fn().mockReturnThis(),
}

const mockBoundsInstance = {}

vi.mock('leaflet', () => ({
  default: {
    map: vi.fn(() => mockMapInstance),
    tileLayer: vi.fn(() => mockTileLayerInstance),
    marker: vi.fn(() => mockMarkerInstance),
    polyline: vi.fn(() => mockPolylineInstance),
    latLngBounds: vi.fn(() => mockBoundsInstance),
    Icon: {
      Default: {
        prototype: {},
        mergeOptions: vi.fn(),
      },
    },
  },
}))

const vuetify = createVuetify({ components, directives })

function mountJourneyView (fromLocation: string, toLocation: string, journeyMode?: JourneyMode) {
  return mount(JourneyView, {
    props: { fromLocation, toLocation, ...(journeyMode ? { journeyMode } : {}) },
    global: { plugins: [vuetify] },
  })
}

function stubFetchSuccess () {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
    json: vi.fn().mockResolvedValue([{ lat: '51.5074', lon: '-0.1278' }]),
  }))
}

function stubFetchError () {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')))
}

describe('JourneyView.vue', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('renders the journey-container div in the DOM', () => {
    stubFetchSuccess()
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    expect(wrapper.find('.journey-container').exists()).toBe(true)
  })

  it('shows loading state before fetches resolve', () => {
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() => new Promise(() => {})))
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    expect(wrapper.findComponent({ name: 'VProgressCircular' }).exists()).toBe(true)
    expect(wrapper.find('.journey-container').classes()).toContain('journey-hidden')
  })

  it('shows map and hides loading state when both geocodings succeed', async () => {
    stubFetchSuccess()
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    expect(wrapper.findComponent({ name: 'VProgressCircular' }).exists()).toBe(false)
    expect(wrapper.find('.journey-container').classes()).not.toContain('journey-hidden')
  })

  it('calls L.marker twice and L.polyline once when geocoding succeeds', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    expect(L.map).toHaveBeenCalledOnce()
    expect(L.marker).toHaveBeenCalledTimes(2)
    expect(L.polyline).toHaveBeenCalledOnce()
  })

  it('calls L.latLngBounds and fitBounds when geocoding succeeds', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    expect(L.latLngBounds).toHaveBeenCalledOnce()
    expect(mockMapInstance.fitBounds).toHaveBeenCalledWith(mockBoundsInstance, { padding: [40, 40] })
  })

  it('shows not-found state when fromLocation geocoding returns empty array', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce({ json: vi.fn().mockResolvedValue([]) })
      .mockResolvedValueOnce({ json: vi.fn().mockResolvedValue([{ lat: '50.9', lon: '-1.4' }]) })
    )
    const wrapper = mountJourneyView('zzz_nonexistent_xyz', 'Southampton, UK')
    await flushPromises()
    expect(wrapper.find('.journey-not-found').exists()).toBe(true)
    expect(wrapper.text()).toContain('Location not found')
  })

  it('shows not-found state when toLocation geocoding returns empty array', async () => {
    vi.stubGlobal('fetch', vi.fn()
      .mockResolvedValueOnce({ json: vi.fn().mockResolvedValue([{ lat: '51.3', lon: '1.3' }]) })
      .mockResolvedValueOnce({ json: vi.fn().mockResolvedValue([]) })
    )
    const wrapper = mountJourneyView('Sandwich, UK', 'zzz_nonexistent_xyz')
    await flushPromises()
    expect(wrapper.find('.journey-not-found').exists()).toBe(true)
    expect(wrapper.text()).toContain('Location not found')
  })

  it('shows error state when fetch throws', async () => {
    stubFetchError()
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    expect(wrapper.find('.journey-error').exists()).toBe(true)
    expect(wrapper.text()).toContain('Map unavailable')
  })

  it('does not call fetch when fromLocation is empty', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    mountJourneyView('', 'Southampton, UK')
    await flushPromises()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('does not call fetch when toLocation is empty', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    mountJourneyView('Sandwich, UK', '')
    await flushPromises()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('calls leafletMap.remove() when component is unmounted', async () => {
    stubFetchSuccess()
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    wrapper.unmount()
    expect(mockMapInstance.remove).toHaveBeenCalled()
  })

  it('re-geocodes and re-creates map when fromLocation prop changes', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    expect(L.map).toHaveBeenCalledTimes(1)

    await wrapper.setProps({ fromLocation: 'London, UK' })
    await flushPromises()
    expect(fetch).toHaveBeenCalledTimes(4)
    expect(L.map).toHaveBeenCalledTimes(2)
  })

  it('re-geocodes and re-creates map when toLocation prop changes', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    expect(L.map).toHaveBeenCalledTimes(1)

    await wrapper.setProps({ toLocation: 'Paris, France' })
    await flushPromises()
    expect(fetch).toHaveBeenCalledTimes(4)
    expect(L.map).toHaveBeenCalledTimes(2)
  })

  it('cleans up existing map before creating a new one on location change', async () => {
    stubFetchSuccess()
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()

    await wrapper.setProps({ fromLocation: 'London, UK' })
    await flushPromises()
    expect(mockMapInstance.remove).toHaveBeenCalledTimes(1)
  })

  it('does not call remove when unmounted without a map being initialized', async () => {
    const fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
    const wrapper = mountJourneyView('', '')
    await flushPromises()
    wrapper.unmount()
    expect(mockMapInstance.remove).not.toHaveBeenCalled()
  })

  it('uses crow-flies style (red dashed) by default', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Sandwich, UK', 'Southampton, UK')
    await flushPromises()
    expect(L.polyline).toHaveBeenCalledWith(
      expect.any(Array),
      expect.objectContaining({ color: 'red', dashArray: '6 4' }),
    )
  })

  it('uses car style (blue solid) when journeyMode is car', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Sandwich, UK', 'Southampton, UK', 'car')
    await flushPromises()
    expect(L.polyline).toHaveBeenCalledWith(
      expect.any(Array),
      expect.objectContaining({ color: '#1565c0' }),
    )
  })

  it('uses walking style (green dotted) when journeyMode is walking', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Sandwich, UK', 'Southampton, UK', 'walking')
    await flushPromises()
    expect(L.polyline).toHaveBeenCalledWith(
      expect.any(Array),
      expect.objectContaining({ color: '#2e7d32' }),
    )
  })

  it('uses train style (orange dashed) when journeyMode is train', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Sandwich, UK', 'Southampton, UK', 'train')
    await flushPromises()
    expect(L.polyline).toHaveBeenCalledWith(
      expect.any(Array),
      expect.objectContaining({ color: '#e65100' }),
    )
  })

  it('uses boat style (teal dashed) when journeyMode is boat', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Sandwich, UK', 'Southampton, UK', 'boat')
    await flushPromises()
    expect(L.polyline).toHaveBeenCalledWith(
      expect.any(Array),
      expect.objectContaining({ color: '#00838f' }),
    )
  })

  it('adds OpenSeaMap tile overlay when journeyMode is boat', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('Southampton, UK', 'Le Havre, France', 'boat')
    await flushPromises()
    expect(L.tileLayer).toHaveBeenCalledWith(
      expect.stringContaining('openseamap.org'),
      expect.objectContaining({ opacity: 0.8 }),
    )
  })

  it('does not add OpenSeaMap tile overlay for non-boat modes', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    mountJourneyView('London, UK', 'Paris, France', 'car')
    await flushPromises()
    const tileLayerCalls = (L.tileLayer as unknown as ReturnType<typeof vi.fn>).mock.calls
    const seaMapCall = tileLayerCalls.find((args: unknown[]) => args[0] === 'https://tiles.openseamap.org/seamark/{z}/{x}/{y}.png')
    expect(seaMapCall).toBeUndefined()
  })

  it('re-renders map when journeyMode prop changes', async () => {
    stubFetchSuccess()
    const L = (await import('leaflet')).default
    const wrapper = mountJourneyView('Sandwich, UK', 'Southampton, UK', 'crow-flies')
    await flushPromises()
    expect(L.map).toHaveBeenCalledTimes(1)

    await wrapper.setProps({ journeyMode: 'car' })
    await flushPromises()
    expect(L.map).toHaveBeenCalledTimes(2)
  })
})
