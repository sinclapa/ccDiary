import { expect, test } from '@playwright/test'

test.describe('Home page', () => {
  test('loads and shows navigation', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load' })
    await expect(page).toHaveTitle(/ccDiary|Cooking Code Diary|Diary/)
    await expect(page.locator('header, [role="banner"]').first()).toBeVisible()
  })

  test('shows login button when not authenticated', async ({ page }) => {
    await page.goto('/')
    const loginBtn = page.locator('#login')
    await expect(loginBtn).toBeVisible()
  })

  test('navigation drawer opens and shows links', async ({ page }) => {
    await page.goto('/')
    await page.locator('button[aria-label="Open navigation"]').first().click()
    await expect(page.getByText('Diaries')).toBeVisible()
    await expect(page.getByText('Home')).toBeVisible()
  })
})
