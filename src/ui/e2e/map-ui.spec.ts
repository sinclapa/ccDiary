import { expect, test } from '@playwright/test'
import { API_BASE } from './config'

const SEEDED_DIARY_TITLE = 'Integration Test Diary'
const SEEDED_ENTRY_YEAR = 1918
const SEEDED_ENTRY_MONTH = 5
const SEEDED_ENTRY_DAY = 21

async function getIntegrationDiaryId (request: import('@playwright/test').APIRequestContext): Promise<string> {
  const response = await request.get(`${API_BASE}/api/v1/Diary/Get`, { ignoreHTTPSErrors: true })
  expect(response.ok()).toBeTruthy()
  const result: { items: Array<{ diaryId: string; title: string }> } = await response.json()
  const match = result.items.find(d => d.title === SEEDED_DIARY_TITLE) ?? result.items[0]
  return match.diaryId
}

function dateStr (year: number, month: number, day: number): string {
  return `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

// ─── Map component — Leaflet rendering ────────────────────────────────────────

test.describe('Map component — Leaflet rendering', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getIntegrationDiaryId(request)
  })

  test('leaflet-container renders inside map-wrapper for a showMap=true entry', async ({ page }) => {
    // May 21, 1918 has at least one entry with showMap=true
    const date = dateStr(SEEDED_ENTRY_YEAR, SEEDED_ENTRY_MONTH, SEEDED_ENTRY_DAY)
    await page.goto(`/diaries/${ww1DiaryId}?date=${date}`, { waitUntil: 'load', timeout: 25000 })

    // Wait for the timeline to load
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    await expect(page.locator('.map-wrapper').first()).toBeVisible({ timeout: 5000 })

    // Leaflet initialises the map container — the .leaflet-container div is added by Leaflet
    await expect(page.locator('.map-wrapper .leaflet-container').first()).toBeVisible({ timeout: 15000 })
  })

  test('map-wrapper is absent when all entries on a date have showMap=false', async ({ page }) => {
    // May 22, 1918 has entries but with showMap=false
    const date = dateStr(SEEDED_ENTRY_YEAR, SEEDED_ENTRY_MONTH, SEEDED_ENTRY_DAY + 1)
    await page.goto(`/diaries/${ww1DiaryId}?date=${date}`, { waitUntil: 'load', timeout: 25000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    await expect(page.locator('.map-wrapper')).toHaveCount(0)
  })
})

// ─── Journey component — Leaflet rendering ────────────────────────────────────

test.describe('Journey component — Leaflet rendering', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getIntegrationDiaryId(request)
  })

  test('leaflet-container renders inside journey-wrapper for a showJourney=true entry', async ({ page }) => {
    // May 21, 1918 has at least one entry with showJourney=true
    const date = dateStr(SEEDED_ENTRY_YEAR, SEEDED_ENTRY_MONTH, SEEDED_ENTRY_DAY)
    await page.goto(`/diaries/${ww1DiaryId}?date=${date}`, { waitUntil: 'load', timeout: 25000 })

    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    await expect(page.locator('.journey-wrapper').first()).toBeVisible({ timeout: 5000 })

    // Leaflet adds .leaflet-container to the journey map element on initialisation
    await expect(page.locator('.journey-wrapper .leaflet-container').first()).toBeVisible({ timeout: 15000 })
  })

  test('journey-wrapper is absent when entry has showJourney=false', async ({ page }) => {
    // May 25, 1918 (SEEDED_ENTRY_DAY + 4) has showJourney=false
    const date = dateStr(SEEDED_ENTRY_YEAR, SEEDED_ENTRY_MONTH, SEEDED_ENTRY_DAY + 4)
    await page.goto(`/diaries/${ww1DiaryId}?date=${date}`, { waitUntil: 'load', timeout: 25000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    await expect(page.locator('.journey-wrapper')).toHaveCount(0)
  })

  test('a page with both a map and a journey renders two separate leaflet maps', async ({ page }) => {
    // May 21, 1918 has one showMap entry and one showJourney entry
    const date = dateStr(SEEDED_ENTRY_YEAR, SEEDED_ENTRY_MONTH, SEEDED_ENTRY_DAY)
    await page.goto(`/diaries/${ww1DiaryId}?date=${date}`, { waitUntil: 'load', timeout: 25000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 20000 })

    // Both wrappers must appear
    await expect(page.locator('.map-wrapper').first()).toBeVisible({ timeout: 10000 })
    await expect(page.locator('.journey-wrapper').first()).toBeVisible({ timeout: 10000 })

    // Each wrapper gets its own Leaflet instance
    await expect(page.locator('.map-wrapper .leaflet-container').first()).toBeVisible({ timeout: 15000 })
    await expect(page.locator('.journey-wrapper .leaflet-container').first()).toBeVisible({ timeout: 15000 })
  })
})
