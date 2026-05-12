import { expect, test } from '@playwright/test'
import { API_BASE } from './config'

// ─── Admin API — unauthenticated access ───────────────────────────────────────

test.describe('Admin API — requires authentication', () => {
  test('GET Admin/Requests returns 401 without a token', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/Admin/Requests`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(401)
  })

  test('PUT Admin/Approve/:id returns 401 without a token', async ({ request }) => {
    const fakeId = '00000000-0000-0000-0000-000000000001'
    const response = await request.put(`${API_BASE}/api/v1/Admin/Approve/${fakeId}`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(401)
  })

  test('PUT Admin/Decline/:id returns 401 without a token', async ({ request }) => {
    const fakeId = '00000000-0000-0000-0000-000000000002'
    const response = await request.put(`${API_BASE}/api/v1/Admin/Decline/${fakeId}`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(401)
  })
})

// ─── Admin page — non-admin access ───────────────────────────────────────────
// The router guard redirects non-admins to /, but auth-store initialisation
// timing means the redirect is not guaranteed within a short window.
// The important invariant is that no admin content is ever visible.

test.describe('Admin page — non-admin access', () => {
  test('Access Requests heading is not visible when not authenticated', async ({ page }) => {
    await page.goto('/admin', { waitUntil: 'load', timeout: 25000 })
    // Guard redirects to / — either way the heading should not appear
    await expect(page.getByRole('heading', { name: 'Access Requests' })).toHaveCount(0)
  })

  test('no approve or decline buttons are shown when not authenticated', async ({ page }) => {
    await page.goto('/admin', { waitUntil: 'load', timeout: 25000 })
    // Router guard redirects non-admins to / — verify the redirect and absence of admin actions
    await expect(page).toHaveURL('/', { timeout: 10000 })
    await expect(page.getByRole('button', { name: 'Approve' })).toHaveCount(0)
    // Note: only Approve is checked — the consent banner may render its own 'Decline' button
  })
})

// ─── Register page ────────────────────────────────────────────────────────────

test.describe('Register page', () => {
  test('loads with the Request Access card and form fields', async ({ page }) => {
    await page.goto('/register', { waitUntil: 'load', timeout: 25000 })
    await expect(page.getByText('Request Access')).toBeVisible({ timeout: 10000 })
    await expect(page.getByLabel('Display Name')).toBeVisible()
    await expect(page.getByLabel('Email')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Submit Request' })).toBeVisible()
  })

  test('shows validation errors when the form is submitted empty', async ({ page }) => {
    await page.goto('/register', { waitUntil: 'load', timeout: 25000 })
    await page.getByRole('button', { name: 'Submit Request' }).click()
    await expect(page.getByText('Name is required')).toBeVisible({ timeout: 5000 })
    await expect(page.getByText('Email is required')).toBeVisible({ timeout: 5000 })
  })

  test('shows validation error for an invalid email format', async ({ page }) => {
    await page.goto('/register', { waitUntil: 'load', timeout: 25000 })
    await page.getByLabel('Display Name').fill('Test User')
    await page.getByLabel('Email').fill('not-an-email')
    await page.getByRole('button', { name: 'Submit Request' }).click()
    await expect(page.getByText('Must be a valid email')).toBeVisible({ timeout: 5000 })
  })

  test('shows success state after submitting a valid access request', async ({ page }) => {
    const uniqueEmail = `e2e-test-${Date.now()}@example-e2e.invalid`
    await page.goto('/register', { waitUntil: 'load', timeout: 25000 })
    await page.getByLabel('Display Name').fill('E2E Test User')
    await page.getByLabel('Email').fill(uniqueEmail)
    await page.getByRole('button', { name: 'Submit Request' }).click()
    await expect(page.getByText('Request Submitted')).toBeVisible({ timeout: 10000 })
    await expect(page.getByText('Your access request has been submitted')).toBeVisible()
  })
})

// ─── Access Request API ───────────────────────────────────────────────────────

test.describe('Access Request API', () => {
  test('POST AccessRequest/Submit returns 201 for a new email address', async ({ request }) => {
    const uniqueEmail = `api-e2e-${Date.now()}@example-e2e.invalid`
    const response = await request.post(`${API_BASE}/api/v1/AccessRequest/Submit`, {
      ignoreHTTPSErrors: true,
      data: { displayName: 'E2E API Test', email: uniqueEmail },
    })
    expect(response.status()).toBe(201)
  })

  test('POST AccessRequest/Submit returns 409 for a duplicate email address', async ({ request }) => {
    const uniqueEmail = `dup-e2e-${Date.now()}@example-e2e.invalid`

    // First submission succeeds
    const first = await request.post(`${API_BASE}/api/v1/AccessRequest/Submit`, {
      ignoreHTTPSErrors: true,
      data: { displayName: 'E2E Dup Test', email: uniqueEmail },
    })
    expect(first.status()).toBe(201)

    // Second submission with the same email is a conflict
    const second = await request.post(`${API_BASE}/api/v1/AccessRequest/Submit`, {
      ignoreHTTPSErrors: true,
      data: { displayName: 'E2E Dup Test Again', email: uniqueEmail },
    })
    expect(second.status()).toBe(409)
  })
})

// ─── Navigation links ─────────────────────────────────────────────────────────

test.describe('Navigation — unauthenticated state', () => {
  test('Admin link is not shown in the header when not authenticated', async ({ page }) => {
    await page.goto('/', { waitUntil: 'load', timeout: 25000 })
    await expect(page.getByRole('link', { name: 'Admin' })).toHaveCount(0)
  })
})
