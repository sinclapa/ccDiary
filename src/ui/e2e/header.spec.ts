import { expect, test } from '@playwright/test'

// ─── Environment badge ─────────────────────────────────────────────────────────

test.describe('Environment badge', () => {
  test('LOCAL badge is visible in the header bar when running on localhost', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
    const badge = page.locator('.app-header__bar .env-badge')
    await expect(badge).toBeVisible({ timeout: 10000 })
  })

  test('badge text is "local" when running on localhost', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
    const badge = page.locator('.app-header__bar .env-badge')
    await expect(badge).toHaveText('local', { timeout: 10000 })
  })

  test('badge has the local variant class', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
    const badge = page.locator('.app-header__bar .env-badge')
    await expect(badge).toHaveClass(/env-badge--local/, { timeout: 10000 })
  })

  test('badge appears between the logo and the desktop nav', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
    const bar = page.locator('.app-header__bar')
    const logo = bar.locator('.logo-link')
    const badge = bar.locator('.env-badge')
    const nav = bar.locator('.desktop-nav')

    const logoBounds = await logo.boundingBox()
    const badgeBounds = await badge.boundingBox()
    const navBounds = await nav.boundingBox()

    expect(logoBounds).not.toBeNull()
    expect(badgeBounds).not.toBeNull()
    expect(navBounds).not.toBeNull()

    // Badge is to the right of the logo and to the left of the desktop nav
    expect(badgeBounds!.x).toBeGreaterThan(logoBounds!.x + logoBounds!.width)
    expect(badgeBounds!.x).toBeLessThan(navBounds!.x + navBounds!.width)
  })
})

// ─── Navigation — Join link ───────────────────────────────────────────────────

test.describe('Navigation — Join link', () => {
  test('Join link is visible in the header when not authenticated', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
    await expect(page.getByRole('link', { name: 'Join' }).first()).toBeVisible({ timeout: 10000 })
  })

  test('Join link navigates to the register page', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
    await page.getByRole('link', { name: 'Join' }).first().click()
    await expect(page).toHaveURL('/register', { timeout: 10000 })
    await expect(page.getByText('Request Access')).toBeVisible({ timeout: 10000 })
  })
})

// ─── Register route — unauthenticated access ──────────────────────────────────

test.describe('Register route — unauthenticated access', () => {
  test('register page is accessible when not authenticated', async ({ page }) => {
    await page.goto('/register', { waitUntil: 'load', timeout: 25000 })
    await expect(page.getByText('Request Access')).toBeVisible({ timeout: 10000 })
    await expect(page.getByLabel('Display Name')).toBeVisible()
    await expect(page.getByLabel('Email')).toBeVisible()
  })

  test('unauthenticated user stays on register page and is not redirected', async ({ page }) => {
    await page.goto('/register', { waitUntil: 'load', timeout: 25000 })
    await expect(page.getByText('Request Access')).toBeVisible({ timeout: 10000 })
    await expect(page).toHaveURL('/register')
  })
})
