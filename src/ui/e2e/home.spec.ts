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

  test('shows navigation links', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('link', { name: 'Diaries' }).first()).toBeVisible()
    await expect(page.getByRole('link', { name: 'Home' }).first()).toBeVisible()
  })
})
