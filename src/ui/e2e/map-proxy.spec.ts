import { expect, test } from '@playwright/test'
import { API_BASE } from './config'

// ─── Geocode API ───────────────────────────────────────────────────────────────

test.describe('Map Tile Proxy — Geocode API', () => {
  test('returns lat/lon for GPS decimal coordinates without any upstream call', async ({ request }) => {
    // Decimal coordinates are resolved locally — no Nominatim request is made
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Geocode`, {
      params: { q: '-10.001389, 39.719972' },
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const body = await response.json()
    expect(body.lat).toBeCloseTo(-10.001389, 4)
    expect(body.lon).toBeCloseTo(39.719972, 4)
  })

  test('returns lat/lon for DMS coordinates without any upstream call', async ({ request }) => {
    // DMS coordinates are parsed locally — no Nominatim request is made
    // 10°00'05.0"S 39°43'11.9"E ≈ lat -10.001389, lon 39.719972
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Geocode`, {
      params: { q: '10\u00b000\'05.0"S 39\u00b043\'11.9"E' },
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const body = await response.json()
    expect(body.lat).toBeCloseTo(-10.001389, 3)
    expect(body.lon).toBeCloseTo(39.719972, 3)
  })

  test('returns 400 for an empty query string', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Geocode`, {
      params: { q: '' },
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(400)
  })

  test('returns 400 when the query parameter is omitted', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Geocode`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(400)
  })

  test('returns 404 for a query that cannot be geocoded', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Geocode`, {
      params: { q: 'zzz_e2e_nonexistent_location_xyz_123' },
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(404)
  })

  test('response has no-store cache header on 404', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Geocode`, {
      params: { q: 'zzz_e2e_nonexistent_location_xyz_456' },
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(404)
    expect(response.headers()['cache-control']).toBe('no-store')
  })

  test('successful response has a public cache-control header', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Geocode`, {
      params: { q: '51.5074, -0.1278' },
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const cacheControl = response.headers()['cache-control'] ?? ''
    expect(cacheControl).toContain('public')
    expect(cacheControl).toContain('max-age')
  })
})

// ─── Tile API ─────────────────────────────────────────────────────────────────

test.describe('Map Tile Proxy — Tile API', () => {
  test('returns image bytes with image content-type for a valid osm tile', async ({ request }) => {
    // Zoom 1 tiles cover large areas and are always available from OpenStreetMap
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Tile/osm/1/0/0`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const contentType = response.headers()['content-type'] ?? ''
    expect(contentType).toContain('image/')
    const body = await response.body()
    expect(body.length).toBeGreaterThan(0)
  })

  test('returns image bytes for openseamap tile source', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Tile/openseamap/1/0/0`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const contentType = response.headers()['content-type'] ?? ''
    expect(contentType).toContain('image/')
  })

  test('returns 404 for an unknown tile source', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Tile/googlemaps/1/0/0`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(404)
  })

  test('successful tile response has a cache-control header', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Tile/osm/1/1/0`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const cacheControl = response.headers()['cache-control'] ?? ''
    expect(cacheControl).toContain('public')
  })
})

// ─── Route API ────────────────────────────────────────────────────────────────

test.describe('Map Tile Proxy — Route API', () => {
  // Two points in central London, ~1 km apart
  const from = { lat: 51.5074, lon: -0.1278 }
  const to = { lat: 51.5155, lon: -0.0922 }

  test('returns a coordinate array for the foot profile', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Route`, {
      params: {
        fromLat: from.lat,
        fromLon: from.lon,
        toLat: to.lat,
        toLon: to.lon,
        profile: 'foot',
      },
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const coords: number[][] = await response.json()
    expect(Array.isArray(coords)).toBe(true)
    expect(coords.length).toBeGreaterThan(1)
    // Each coordinate should be a [lat, lon] pair
    for (const c of coords) {
      expect(c).toHaveLength(2)
      expect(typeof c[0]).toBe('number')
      expect(typeof c[1]).toBe('number')
    }
  })

  test('returns a coordinate array for the driving profile', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Route`, {
      params: {
        fromLat: from.lat,
        fromLon: from.lon,
        toLat: to.lat,
        toLon: to.lon,
        profile: 'driving',
      },
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const coords: number[][] = await response.json()
    expect(Array.isArray(coords)).toBe(true)
    expect(coords.length).toBeGreaterThan(1)
  })

  test('returns 404 for an invalid profile name', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/MapTile/Route`, {
      params: {
        fromLat: from.lat,
        fromLon: from.lon,
        toLat: to.lat,
        toLon: to.lon,
        profile: 'bike',
      },
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(404)
  })
})
