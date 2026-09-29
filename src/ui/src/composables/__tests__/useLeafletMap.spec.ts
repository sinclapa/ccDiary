import { mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, nextTick, reactive } from 'vue'

vi.mock('leaflet', () => ({
  default: {
    Icon: { Default: { prototype: { _getIconUrl: () => '' }, mergeOptions: vi.fn() } },
  },
}))

// every ResizeObserver created, with what it watches, so a resize can be replayed
const observers: { callback: () => void, target?: Element, disconnect: ReturnType<typeof vi.fn> }[] = []

function fakeMap () {
  return { remove: vi.fn(), invalidateSize: vi.fn() }
}

async function withComposable (props: { height?: string } = {}) {
  const { useLeafletMap } = await import('../useLeafletMap')
  let api!: ReturnType<typeof useLeafletMap>
  const wrapper = mount(defineComponent({
    setup () {
      api = useLeafletMap(props)
      return () => h('div', { ref: api.mapContainer })
    },
  }))
  await nextTick()
  return { api, wrapper }
}

describe('useLeafletMap', () => {
  beforeEach(() => {
    observers.length = 0
    vi.stubGlobal('ResizeObserver', vi.fn(function (this: any, callback: () => void) {
      this.callback = callback
      this.observe = vi.fn((target: Element) => {
        this.target = target
      })
      this.disconnect = vi.fn()
      observers.push(this)
    }))
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('points Leaflet at the bundled marker images, once, when first imported', async () => {
    const L = (await import('leaflet')).default
    await import('../useLeafletMap')
    expect(L.Icon.Default.mergeOptions).toHaveBeenCalledWith(expect.objectContaining({
      iconUrl: expect.any(String),
      iconRetinaUrl: expect.any(String),
      shadowUrl: expect.any(String),
    }))
    expect((L.Icon.Default.prototype as any)._getIconUrl).toBeUndefined()
  })

  it('starts loading, with no height of its own', async () => {
    const { api } = await withComposable()
    expect(api.status.value).toBe('loading')
    expect(api.heightStyle.value).toBeUndefined()
  })

  it('follows the height it is given', async () => {
    const props = reactive<{ height?: string }>({ height: '600px' })
    const { api } = await withComposable(props)
    expect(api.heightStyle.value).toEqual({ height: '600px' })
    props.height = undefined
    expect(api.heightStyle.value).toBeUndefined()
  })

  it('keeps an adopted map measured as its box changes', async () => {
    const { api } = await withComposable()
    const map = fakeMap()

    expect(api.setMap(map as any)).toBe(map)
    expect(observers).toHaveLength(1)
    expect(observers[0].target).toBe(api.mapContainer.value)
    observers[0].callback()
    expect(map.invalidateSize).toHaveBeenCalledOnce()
  })

  it('replaces the watcher when another map is adopted', async () => {
    const { api } = await withComposable()
    api.setMap(fakeMap() as any)
    api.setMap(fakeMap() as any)
    expect(observers[0].disconnect).toHaveBeenCalled()
    expect(observers).toHaveLength(2)
  })

  it('does not watch before the container exists', async () => {
    const { useLeafletMap } = await import('../useLeafletMap')
    let api!: ReturnType<typeof useLeafletMap>
    mount(defineComponent({
      setup () {
        api = useLeafletMap({})
        return () => h('span')
      },
    }))
    api.setMap(fakeMap() as any)
    expect(observers).toHaveLength(0)
  })

  it('clearMap removes the map and stops watching', async () => {
    const { api } = await withComposable()
    const map = fakeMap()
    api.setMap(map as any)

    api.clearMap()

    expect(map.remove).toHaveBeenCalledOnce()
    expect(observers[0].disconnect).toHaveBeenCalled()
    api.clearMap()
    expect(map.remove).toHaveBeenCalledOnce()
  })

  it('removes the map when its component goes', async () => {
    const { api, wrapper } = await withComposable()
    const map = fakeMap()
    api.setMap(map as any)

    wrapper.unmount()

    expect(map.remove).toHaveBeenCalledOnce()
  })
})
