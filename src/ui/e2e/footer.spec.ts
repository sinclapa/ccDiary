import { expect, test } from '@playwright/test'

test.describe('App Footer', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
  })

  test('brand logo renders as inline SVG in footer', async ({ page }) => {
    const brandSvg = page.locator('footer a[href="https://cookingcode.com"] svg')
    await expect(brandSvg).toBeVisible({ timeout: 10000 })
  })

  test('brand link opens CookingCode site in new tab', async ({ page }) => {
    const brandLink = page.locator('footer a[href="https://cookingcode.com"]')
    await expect(brandLink).toBeVisible({ timeout: 10000 })
    await expect(brandLink).toHaveAttribute('target', '_blank')
    await expect(brandLink).toHaveAttribute('title', 'CookingCode')
  })

  test('displays app version with semver format', async ({ page }) => {
    const versionSpan = page.locator('footer .footer-row--secondary span').first()
    await expect(versionSpan).toBeVisible({ timeout: 10000 })
    const versionText = (await versionSpan.textContent())?.trim() ?? ''
    expect(versionText).toMatch(/^\d+\.\d+\.\d+/)
  })

  test('GitHub social link is present', async ({ page }) => {
    const githubLink = page.locator('footer a[href*="github.com/sinclapa/ccDiary"]')
    await expect(githubLink).toBeVisible({ timeout: 10000 })
    await expect(githubLink).toHaveAttribute('target', '_blank')
  })

  test('shows cookie preferences button', async ({ page }) => {
    const cookieBtn = page.locator('footer button', { hasText: 'Cookie preferences' })
    await expect(cookieBtn).toBeVisible({ timeout: 10000 })
    await expect(cookieBtn).toContainText('Cookie preferences')
  })

  test('copyright text is present with current year', async ({ page }) => {
    const year = new Date().getFullYear().toString()
    const footer = page.locator('footer')
    await expect(footer).toContainText(year, { timeout: 10000 })
    await expect(footer).toContainText('Cooking Code')
  })
})
