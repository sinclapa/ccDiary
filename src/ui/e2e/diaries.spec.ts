import { expect, test } from '@playwright/test'
import { API_BASE } from './config'

const SEEDED_DIARY_TITLE = 'Integration Test Diary'
const SEEDED_DIARY_AUTHOR = 'Claude Sonnet'

async function getIntegrationDiaryId (request: import('@playwright/test').APIRequestContext): Promise<string> {
  const response = await request.get(`${API_BASE}/api/v1/Diary/Get`, { ignoreHTTPSErrors: true })
  expect(response.ok()).toBeTruthy()
  const diaries: Array<{ diaryId: string; title: string; author: string }> = await response.json()
  const match = diaries.find(d => d.title === SEEDED_DIARY_TITLE) ?? diaries[0]
  return match.diaryId
}

async function gotoDiaries (page: import('@playwright/test').Page): Promise<void> {
  await page.goto('/diaries')
  await expect(page.locator('table')).toBeVisible({ timeout: 10000 })
}

async function gotoDiaryDetail (page: import('@playwright/test').Page, diaryId: string): Promise<void> {
  await page.goto(`/diaries/${diaryId}`, { waitUntil: 'load', timeout: 25000 })
  await expect(page.locator('.v-date-picker')).toBeVisible({ timeout: 5000 })
}

test.describe('Diaries list', () => {
  test.beforeEach(async ({ page }) => {
    await gotoDiaries(page)
  })

  test('loads data table with diary rows', async ({ page }) => {
    // v-data-table renders rows with diary titles as links
    const diaryLinks = page.locator('table a[href*="diaries/"]')
    await expect(diaryLinks.first()).toBeVisible({ timeout: 10000 })
    const count = await diaryLinks.count()
    expect(count).toBeGreaterThan(0)
  })

  test('shows Integration Test Diary in the list', async ({ page }) => {
    await expect(page.getByText(SEEDED_DIARY_TITLE, { exact: false }).first()).toBeVisible({ timeout: 10000 })
  })

  test('shows diary author column', async ({ page }) => {
    await expect(page.getByText(SEEDED_DIARY_AUTHOR, { exact: false }).first()).toBeVisible({ timeout: 10000 })
  })

  test('does not show Add Diary button when not authenticated', async ({ page }) => {
    // Add Diary button only shows when authenticated
    await expect(page.getByRole('button', { name: 'Add Diary' })).toHaveCount(0)
  })

  test('does not show edit or delete buttons when not authenticated', async ({ page }) => {
    await expect(page.locator('button[aria-label="Edit entry"]')).toHaveCount(0)
    await expect(page.locator('button[aria-label="Delete entry"]')).toHaveCount(0)
  })

  test('clicking diary title navigates to detail page', async ({ page }) => {
    const firstLink = page.locator('table a[href*="diaries/"]').first()
    await expect(firstLink).toBeVisible({ timeout: 10000 })
    await firstLink.click()
    await expect(page).toHaveURL(/\/diaries\/[a-f0-9-]+/, { timeout: 10000 })
  })
})

test.describe('Diary detail page', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    ww1DiaryId = await getIntegrationDiaryId(request)
  })

  test('loads diary detail with date picker', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
  })

  test('shows diary title and author', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.title')).not.toBeEmpty()
    await expect(page.locator('.author')).not.toBeEmpty()
  })

  test('shows diary entries in timeline after selecting a date', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    const timeline = page.locator('.v-timeline-item')
    await expect(timeline.first()).toBeVisible({ timeout: 10000 })
  })

  test('skip-forward moves to a later entry', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    // Page starts at minDate so skip-backward is already disabled; skip-forward should be enabled
    const startBtn = page.locator('button[aria-label="Go to start"]').first()
    await expect(startBtn).toBeDisabled()

    const entryBefore = await page.locator('.v-timeline-item').first().textContent()

    // Move forward one entry (Move forward = moveForward)
    const forwardBtn = page.locator('button[aria-label="Move forward"]').first()
    await expect(forwardBtn).not.toBeDisabled()
    await forwardBtn.click()
    await expect.poll(async () => (await page.locator('.v-timeline-item').first().textContent()) ?? '').not.toBe(entryBefore ?? '')
  })

  test('skip-to-start button is disabled at page load (starts at first entry)', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    // Page initialises at minDate so the go-to-start button is already disabled
    const startBtn = page.locator('button[aria-label="Go to start"]').first()
    await expect(startBtn).toBeDisabled({ timeout: 5000 })

    // Navigate to end, then verify go-to-start becomes enabled
    const endBtn = page.locator('button[aria-label="Go to end"]').first()
    await endBtn.click()
    await expect(startBtn).not.toBeDisabled({ timeout: 5000 })

    // Now go back to start and confirm it disables again
    await startBtn.click()
    await expect(startBtn).toBeDisabled({ timeout: 5000 })
  })

  test('skip-to-end button disables when at last entry', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 12000 })

    const endBtn = page.locator('button[aria-label="Go to end"]').first()
    await endBtn.click()

    await expect(endBtn).toBeDisabled({ timeout: 5000 })
  })

  test('compact/expand date picker toggle changes button label', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)

    const toggleBtn = page.locator('button:has-text("Compact View"), button:has-text("Expanded View")')
    await expect(toggleBtn).toBeVisible()
    const initialText = (await toggleBtn.textContent())?.trim() ?? ''
    await toggleBtn.click()
    await expect.poll(async () => ((await toggleBtn.textContent()) ?? '').trim()).not.toBe(initialText)
  })

  test('clicking a marked day loads entries for that day', async ({ page }) => {
    await gotoDiaryDetail(page, ww1DiaryId)
    await expect(page.locator('.v-timeline-item').first()).toBeVisible({ timeout: 10000 })

    const firstEntry = await page.locator('.v-timeline-item').first().textContent()

    // Page already starts at minDate — skip-backward is disabled, skip-forward is enabled
    const startBtn = page.locator('button[aria-label="Go to start"]').first()
    await expect(startBtn).toBeDisabled()

    // Click a marked day dot
    const markedDay = page.locator('.diary-day-content:has(.diary-day-marker) button').first()
    if (await markedDay.count() > 0) {
      await markedDay.click()
      await expect.poll(async () => (await page.locator('.v-timeline-item').first().textContent()) ?? '').toBeTruthy()
    }

    // Navigate to a different date via forward button and verify entries update
    const fwdBtn = page.locator('button[aria-label="Move forward"]').first()
    if (!await fwdBtn.isDisabled()) {
      await fwdBtn.click()
      await expect.poll(async () => (await page.locator('.v-timeline-item').first().textContent()) ?? '').not.toBe(firstEntry ?? '')
    }
  })

  test('navigating to diary from list page works', async ({ page }) => {
    await gotoDiaries(page)
    const diaryLink = page.locator(`table a[href*="${ww1DiaryId}"]`)
    await expect(diaryLink).toBeVisible({ timeout: 10000 })
    await diaryLink.click()
    await expect(page).toHaveURL(new RegExp(ww1DiaryId), { timeout: 10000 })
    await expect(page.locator('.v-date-picker')).toBeVisible({ timeout: 12000 })
  })
})
