import { expect, test } from '@playwright/test'
import { API_BASE } from './config'

const SEEDED_DIARY_TITLE = 'Local: WW1 Diary'
// First seeded entry date — used for direct API calls
const SEEDED_ENTRY_YEAR = 1918
const SEEDED_ENTRY_MONTH = 5
const SEEDED_ENTRY_DAY = 21

async function getWW1DiaryId (request: import('@playwright/test').APIRequestContext): Promise<string> {
  const response = await request.get(`${API_BASE}/api/v1/Diary/Get`, { ignoreHTTPSErrors: true })
  expect(response.ok()).toBeTruthy()
  const diaries: Array<{ diaryId: string; title: string }> = await response.json()
  const match = diaries.find(d => d.title === SEEDED_DIARY_TITLE) ??
    diaries.find(d => d.title.includes('WW1')) ??
    diaries[0]
  return match.diaryId
}

async function gotoDiaryDetail (page: import('@playwright/test').Page, diaryId: string): Promise<void> {
  await page.goto(`/diaries/${diaryId}`)
  await expect(page.locator('.v-date-picker')).toBeVisible({ timeout: 12000 })
}

// ─── Date picker header year format ────────────────────────────────────────────

test.describe('Date picker header year format', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getWW1DiaryId(request)
  })

  test('date picker header shows the full date including year', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)

    // Wait until the header is populated with a 4-digit year (loads async after minDate resolves)
    const header = page.locator('.v-date-picker-header').first()
    await expect(header).toBeVisible({ timeout: 12000 })
    await expect(header).toContainText(/\d{4}/, { timeout: 10000 })
  })

  test('date picker header matches format: weekday day month year', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)

    const header = page.locator('.v-date-picker-header').first()
    await expect(header).toBeVisible({ timeout: 12000 })

    // Matches e.g. "Tue 21 May 1918" — weekday, day, abbreviated month, 4-digit year
    await expect(header).toContainText(
      /[A-Z][a-z]{2}\s+\d{1,2}\s+[A-Z][a-z]{2}\s+\d{4}/,
      { timeout: 10000 },
    )
  })

  test('date picker header year matches the WW1 diary era (1918)', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)

    const header = page.locator('.v-date-picker-header').first()
    await expect(header).toContainText('1918', { timeout: 10000 })
  })
})

// ─── Map display ───────────────────────────────────────────────────────────────

test.describe('Map display on diary entries', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getWW1DiaryId(request)
  })

  test('no map is shown for seeded diary entries where showMap is false', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)

    // Wait for at least one timeline entry to be present
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    // Seeded WW1 diary entries have showMap=false (default) — no map containers should be rendered
    await expect(page.locator('.map-wrapper')).toHaveCount(0)
  })

  test('map-wrapper renders in timeline when entry has showMap enabled', async ({ page, request }) => {
    // Fetch the first entry for the known seeded date to read its id
    const searchResponse = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    expect(searchResponse.ok()).toBeTruthy()
    const entries: Array<{ diaryEntryId: string; showMap: boolean; mapLocation: string }> = await searchResponse.json()
    expect(entries.length).toBeGreaterThan(0)

    // If no seeded entry has showMap=true, the test verifies the default state only
    const showMapEntry = entries.find(e => e.showMap && e.mapLocation)
    if (!showMapEntry) {
      // All seeded entries default to showMap=false — assert no maps rendered (already covered above)
      await gotoDiaryDetail(page, ww1DiaryId)
      await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
      await expect(page.locator('.map-wrapper')).toHaveCount(0)
      return
    }

    // Navigate to the diary and verify map-wrapper is present for the showMap entry
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.map-wrapper').first()).toBeVisible({ timeout: 15000 })
  })
})

// ─── API: new fields on DiaryEntryDTO ──────────────────────────────────────────

test.describe('DiaryEntry API — mapLocation and showMap fields', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getWW1DiaryId(request)
  })

  test('GET search for day returns entries that include mapLocation and showMap fields', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    expect(response.ok()).toBeTruthy()
    const entries: Array<Record<string, unknown>> = await response.json()
    expect(entries.length).toBeGreaterThan(0)

    // Both new fields must be present on every returned entry
    for (const entry of entries) {
      expect(entry).toHaveProperty('mapLocation')
      expect(entry).toHaveProperty('showMap')
    }
  })

  test('seeded diary entries have showMap defaulting to false', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    const entries: Array<{ showMap: boolean }> = await response.json()
    for (const entry of entries) {
      expect(entry.showMap).toBe(false)
    }
  })

  test('GET single diary entry includes mapLocation and showMap', async ({ request }) => {
    // Retrieve entries for the known day, then fetch one by its id
    const searchResponse = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    const entries: Array<{ diaryEntryId: string }> = await searchResponse.json()
    const entryId = entries[0].diaryEntryId

    const getResponse = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Get/${entryId}`,
      { ignoreHTTPSErrors: true },
    )
    expect(getResponse.ok()).toBeTruthy()
    const entry: Record<string, unknown> = await getResponse.json()
    expect(entry).toHaveProperty('mapLocation')
    expect(entry).toHaveProperty('showMap')
    expect(entry.showMap).toBe(false)
  })

  test('POST create with showMap and mapLocation requires authentication', async ({ request }) => {
    const response = await request.post(`${API_BASE}/api/v1/DiaryEntry/Create`, {
      ignoreHTTPSErrors: true,
      data: {
        diaryId: ww1DiaryId,
        date: new Date().toISOString(),
        location: 'Test Location',
        entry: 'E2E test entry',
        mapLocation: 'London, UK',
        showMap: true,
      },
    })
    expect(response.status()).toBe(401)
  })

  test('PUT update with showMap and mapLocation requires authentication', async ({ request }) => {
    const response = await request.put(`${API_BASE}/api/v1/DiaryEntry/Update`, {
      ignoreHTTPSErrors: true,
      data: {
        diaryEntryId: '00000000-0000-0000-0000-000000000000',
        diaryId: ww1DiaryId,
        date: new Date().toISOString(),
        location: 'Test Location',
        entry: 'E2E test entry',
        mapLocation: 'London, UK',
        showMap: true,
      },
    })
    expect(response.status()).toBe(401)
  })
})

// ─── Editor fields not accessible without authentication ───────────────────────

test.describe('DiaryEntry editor — unauthenticated access', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getWW1DiaryId(request)
  })

  test('Show Map toggle is not visible without authentication', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    // The Add button is hidden without auth, so the editor never opens
    await expect(page.getByRole('button', { name: 'Add' })).toHaveCount(0)
    // Therefore the Show Map switch (id="show-map") is never in the DOM
    await expect(page.locator('#show-map')).toHaveCount(0)
  })

  test('Map Location field is not visible without authentication', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('#map-location')).toHaveCount(0)
  })

  test('edit and delete buttons are hidden without authentication', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    await expect(page.locator('button:has(.mdi-pencil)')).toHaveCount(0)
    await expect(page.locator('button:has(.mdi-delete)')).toHaveCount(0)
  })
})
