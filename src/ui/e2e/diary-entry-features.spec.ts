import { expect, test } from '@playwright/test'
import { API_BASE } from './config'

const SEEDED_DIARY_TITLE = 'WW1 Diary'
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
  await page.goto(`/diaries/${diaryId}`, { waitUntil: 'networkidle', timeout: 25000 })
  await expect(page.locator('.v-date-picker')).toBeVisible({ timeout: 5000 })
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

  test('no map is shown for diary entries where showMap is false', async ({ page }) => {
    // Navigate to May 22 which has showMap=false entries (unlike May 21 which has showMap=true)
    const dateNoMap = `${SEEDED_ENTRY_YEAR}-${String(SEEDED_ENTRY_MONTH).padStart(2, '0')}-${String(SEEDED_ENTRY_DAY + 1).padStart(2, '0')}`
    await page.goto(`/diaries/${ww1DiaryId}?date=${dateNoMap}`)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
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

  test('seeded diary entries on May 22 have showMap=false', async ({ request }) => {
    // May 22 entries have showMap=false; May 21 entries have showMap=true (seeded explicitly)
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY + 1}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    const entries: Array<{ showMap: boolean }> = await response.json()
    expect(entries.length).toBeGreaterThan(0)
    for (const entry of entries) {
      expect(entry.showMap).toBe(false)
    }
  })

  test('at least one seeded diary entry on May 21 has showMap=true with mapLocation set', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    const entries: Array<{ showMap: boolean; mapLocation: string }> = await response.json()
    expect(entries.length).toBeGreaterThan(0)
    const showMapEntry = entries.find(e => e.showMap && e.mapLocation)
    expect(showMapEntry).toBeDefined()
    expect(showMapEntry?.mapLocation).toBeTruthy()
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
    // May 21 seeded entries have showMap=true
    expect(typeof entry.showMap).toBe('boolean')
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

// ─── API: showJourney, fromLocation, toLocation fields ────────────────────────

test.describe('DiaryEntry API — showJourney, fromLocation, toLocation fields', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getWW1DiaryId(request)
  })

  test('GET search for day returns entries that include showJourney, fromLocation, toLocation fields', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    expect(response.ok()).toBeTruthy()
    const entries: Array<Record<string, unknown>> = await response.json()
    expect(entries.length).toBeGreaterThan(0)

    for (const entry of entries) {
      expect(entry).toHaveProperty('showJourney')
      expect(entry).toHaveProperty('fromLocation')
      expect(entry).toHaveProperty('toLocation')
    }
  })

  test('seeded May 21 entry has showJourney=true with fromLocation and toLocation set', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    const entries: Array<{ showJourney: boolean; fromLocation: string; toLocation: string }> = await response.json()
    expect(entries.length).toBeGreaterThan(0)
    const journeyEntry = entries.find(e => e.showJourney)
    expect(journeyEntry).toBeDefined()
    expect(journeyEntry?.fromLocation).toBeTruthy()
    expect(journeyEntry?.toLocation).toBeTruthy()
  })

  test('seeded May 22 entries have showJourney=true', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY + 1}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    const entries: Array<{ showJourney: boolean }> = await response.json()
    expect(entries.length).toBeGreaterThan(0)
    for (const entry of entries) {
      expect(entry.showJourney).toBe(true)
    }
  })

  test('POST create with showJourney requires authentication', async ({ request }) => {
    const response = await request.post(`${API_BASE}/api/v1/DiaryEntry/Create`, {
      ignoreHTTPSErrors: true,
      data: {
        diaryId: ww1DiaryId,
        date: new Date().toISOString(),
        location: 'Test Location',
        entry: 'E2E test entry',
        showJourney: true,
        fromLocation: 'London, UK',
        toLocation: 'Paris, France',
      },
    })
    expect(response.status()).toBe(401)
  })
})

// ─── Journey map display ───────────────────────────────────────────────────────

test.describe('Journey map display on diary entries', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getWW1DiaryId(request)
  })

  test('no journey-wrapper shown when showJourney is false', async ({ page }) => {
    // May 25 (SEEDED_ENTRY_DAY + 4) has a single entry with showJourney=false
    const dateNoJourney = `${SEEDED_ENTRY_YEAR}-${String(SEEDED_ENTRY_MONTH).padStart(2, '0')}-${String(SEEDED_ENTRY_DAY + 4).padStart(2, '0')}`
    await page.goto(`/diaries/${ww1DiaryId}?date=${dateNoJourney}`, { waitUntil: 'networkidle', timeout: 25000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 5000 })
    await expect(page.locator('.journey-wrapper')).toHaveCount(0)
  })

  test('journey-wrapper renders in timeline when entry has showJourney enabled', async ({ page, request }) => {
    const searchResponse = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/Search/${ww1DiaryId}/${SEEDED_ENTRY_YEAR}/${SEEDED_ENTRY_MONTH}/${SEEDED_ENTRY_DAY}`,
      { ignoreHTTPSErrors: true, headers: { 'x-utc-offset': '0' } },
    )
    expect(searchResponse.ok()).toBeTruthy()
    const entries: Array<{ diaryEntryId: string; showJourney: boolean; fromLocation: string; toLocation: string }> = await searchResponse.json()
    expect(entries.length).toBeGreaterThan(0)

    const journeyEntry = entries.find(e => e.showJourney && e.fromLocation && e.toLocation)
    if (!journeyEntry) {
      await gotoDiaryDetail(page, ww1DiaryId)
      await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
      await expect(page.locator('.journey-wrapper')).toHaveCount(0)
      return
    }

    const dateStr = `${SEEDED_ENTRY_YEAR}-${String(SEEDED_ENTRY_MONTH).padStart(2, '0')}-${String(SEEDED_ENTRY_DAY).padStart(2, '0')}`
    await page.goto(`/diaries/${ww1DiaryId}?date=${dateStr}`, { waitUntil: 'networkidle', timeout: 25000 })
    await expect(page.locator('.journey-wrapper').first()).toBeVisible({ timeout: 5000 })
  })
})

// ─── URL date bookmarking ──────────────────────────────────────────────────────

test.describe('URL date bookmarking', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getWW1DiaryId(request)
  })

  test('URL contains date query param after diary page loads', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    await expect(page).toHaveURL(/[?&]date=\d{4}-\d{2}-\d{2}/, { timeout: 5000 })
  })

  test('navigating directly to URL with date param loads that date (bookmarkability)', async ({ page }) => {
    const targetDate = `${SEEDED_ENTRY_YEAR}-${String(SEEDED_ENTRY_MONTH).padStart(2, '0')}-${String(SEEDED_ENTRY_DAY).padStart(2, '0')}`
    await page.goto(`/diaries/${ww1DiaryId}?date=${targetDate}`)
    await expect(page.locator('.v-date-picker')).toBeVisible({ timeout: 12000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    // Date picker header should reflect the bookmarked date
    const header = page.locator('.v-date-picker-header').first()
    await expect(header).toContainText('1918', { timeout: 10000 })
    await expect(header).toContainText('May', { timeout: 10000 })
    await expect(header).toContainText('21', { timeout: 10000 })
  })

  test('forward navigation updates the URL date', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    const initialUrl = page.url()
    const initialDateMatch = initialUrl.match(/[?&]date=(\d{4}-\d{2}-\d{2})/)
    expect(initialDateMatch).not.toBeNull()
    const initialDate = initialDateMatch![1]

    const forwardBtn = page.locator('button:has(.mdi-fast-forward)').first()
    await expect(forwardBtn).not.toBeDisabled()
    await forwardBtn.click()

    await expect.poll(() => {
      const match = page.url().match(/[?&]date=(\d{4}-\d{2}-\d{2})/)
      return match ? match[1] : null
    }, { timeout: 8000 }).not.toBe(initialDate)
  })

  test('browser back and forward navigates between bookmarked dates and reloads entries', async ({ page }) => {
    // Seed data has entries on May 21 and May 22, 1918
    const dateA = `${SEEDED_ENTRY_YEAR}-${String(SEEDED_ENTRY_MONTH).padStart(2, '0')}-${String(SEEDED_ENTRY_DAY).padStart(2, '0')}`
    const dateB = `${SEEDED_ENTRY_YEAR}-${String(SEEDED_ENTRY_MONTH).padStart(2, '0')}-${String(SEEDED_ENTRY_DAY + 1).padStart(2, '0')}`

    // Visit date A — adds to browser history
    await page.goto(`/diaries/${ww1DiaryId}?date=${dateA}`)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    // Visit date B — adds another history entry
    await page.goto(`/diaries/${ww1DiaryId}?date=${dateB}`)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    // Date picker header must reflect date B
    const header = page.locator('.v-date-picker-header').first()
    await expect(header).toContainText(String(SEEDED_ENTRY_DAY + 1), { timeout: 10000 })

    // Browser back → should return to date A, entries must reload via the route watcher
    await page.goBack()
    await expect(page).toHaveURL(new RegExp(`date=${dateA}`), { timeout: 8000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    await expect(header).toContainText(String(SEEDED_ENTRY_DAY), { timeout: 10000 })

    // Browser forward → should go back to date B
    await page.goForward()
    await expect(page).toHaveURL(new RegExp(`date=${dateB}`), { timeout: 8000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
  })

  test('forward nav button uses replace — back skips over intermediate dates to the list', async ({ page }) => {
    // Navigate from list so the list page is the history entry behind the diary
    await page.goto('/diaries')
    await expect(page.locator('table')).toBeVisible({ timeout: 10000 })
    await page.locator(`table a[href*="${ww1DiaryId}"]`).click()
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    // Use forward navigation (router.replace — should not add a history entry)
    const forwardBtn = page.locator('button:has(.mdi-fast-forward)').first()
    await expect(forwardBtn).not.toBeDisabled()
    await forwardBtn.click()
    await expect.poll(() => page.url()).toMatch(/[?&]date=\d{4}-\d{2}-\d{2}/)

    // Browser back should return to /diaries (the list), not to a skipped/intermediate date
    await page.goBack()
    await expect(page).toHaveURL(/\/diaries$/, { timeout: 8000 })
  })

  test('out-of-range date in URL is clamped to diary bounds', async ({ page }) => {
    // A date far in the future — should be clamped to maxDate
    await page.goto(`/diaries/${ww1DiaryId}?date=2099-01-01`)
    await expect(page.locator('.v-date-picker')).toBeVisible({ timeout: 12000 })
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })
    // URL date should be clamped to the diary's maxDate (within 1918–1919 range for WW1)
    await expect(page).toHaveURL(/[?&]date=191[89]-/, { timeout: 5000 })
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

  test('Show Journey toggle is not visible without authentication', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.getByRole('button', { name: 'Add' })).toHaveCount(0)
    await expect(page.locator('#show-journey')).toHaveCount(0)
  })

  test('From/To Location fields are not visible without authentication', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('#from-location')).toHaveCount(0)
    await expect(page.locator('#to-location')).toHaveCount(0)
  })
})
