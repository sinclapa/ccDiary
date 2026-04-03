import { expect, test } from '@playwright/test'
import { API_BASE } from './config'

test.describe('API health and info endpoints', () => {
  test('actuator health returns UP', async ({ request }) => {
    const response = await request.get(`${API_BASE}/actuator/health`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const body = await response.json()
    expect(body.status).toBe('UP')
    expect(body.details?.db?.status).toBe('UP')
  })

  test('actuator info endpoint responds', async ({ request }) => {
    const response = await request.get(`${API_BASE}/actuator/info`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
  })

  test('assembly info endpoint returns version data', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/assembly-info`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const body = await response.json()
    expect(body).toHaveProperty('assemblyName')
    expect(body).toHaveProperty('assemblyVersion')
  })

  test('swagger UI is accessible', async ({ page }) => {
    await page.goto(`${API_BASE}/swagger/index.html`, { waitUntil: 'domcontentloaded' })
    await expect(page.locator('#swagger-ui')).toBeVisible({ timeout: 15000 })
  })

  test('swagger JSON spec is valid', async ({ request }) => {
    const response = await request.get(`${API_BASE}/swagger/v1/swagger.json`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const spec = await response.json()
    expect(spec).toHaveProperty('openapi')
    expect(spec).toHaveProperty('paths')
    expect(Object.keys(spec.paths).length).toBeGreaterThan(0)
  })
})

test.describe('Diary API', () => {
  test('GET /api/v1/Diary/Get returns diary list', async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/Diary/Get`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const diaries = await response.json()
    expect(Array.isArray(diaries)).toBeTruthy()
    expect(diaries.length).toBeGreaterThan(0)
    expect(diaries[0]).toHaveProperty('diaryId')
    expect(diaries[0]).toHaveProperty('title')
    expect(diaries[0]).toHaveProperty('author')
  })

  test('GET /api/v1/Diary/Get/:id returns single diary', async ({ request }) => {
    // First get the list
    const listResponse = await request.get(`${API_BASE}/api/v1/Diary/Get`, {
      ignoreHTTPSErrors: true,
    })
    const diaries = await listResponse.json()
    const id = diaries[0].diaryId

    const response = await request.get(`${API_BASE}/api/v1/Diary/Get/${id}`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.ok()).toBeTruthy()
    const diary = await response.json()
    expect(diary.diaryId).toBe(id)
  })

  test('POST /api/v1/Diary/Create returns 401 without auth', async ({ request }) => {
    const response = await request.post(`${API_BASE}/api/v1/Diary/Create`, {
      ignoreHTTPSErrors: true,
      data: { title: 'Test', author: 'Test Author', description: '' },
    })
    expect(response.status()).toBe(401)
  })

  test('PUT /api/v1/Diary/Update returns 401 without auth', async ({ request }) => {
    const response = await request.put(`${API_BASE}/api/v1/Diary/Update`, {
      ignoreHTTPSErrors: true,
      data: { diaryId: '00000000-0000-0000-0000-000000000000', title: 'Test', author: 'Test Author', description: '' },
    })
    expect(response.status()).toBe(401)
  })

  test('DELETE /api/v1/Diary/Delete returns 401 without auth', async ({ request }) => {
    const response = await request.delete(`${API_BASE}/api/v1/Diary/Delete/00000000-0000-0000-0000-000000000000`, {
      ignoreHTTPSErrors: true,
    })
    expect(response.status()).toBe(401)
  })
})

test.describe('DiaryEntry API', () => {
  let ww1DiaryId: string

  test.beforeAll(async ({ request }) => {
    const response = await request.get(`${API_BASE}/api/v1/Diary/Get`, { ignoreHTTPSErrors: true })
    const diaries: Array<{ diaryId: string; title: string }> = await response.json()
    const ww1 = diaries.find(d => d.title.includes('WW1 Diary') && d.title.includes('Sapper')) ??
      diaries.find(d => d.title.includes('WW1')) ??
      diaries[0]
    ww1DiaryId = ww1.diaryId
  })

  test('GET max date for diary returns a date', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/GetMaxDate/${ww1DiaryId}`,
      { ignoreHTTPSErrors: true },
    )
    expect(response.ok()).toBeTruthy()
    const date = await response.text()
    expect(new Date(JSON.parse(date)).getTime()).not.toBeNaN()
  })

  test('GET min date for diary returns a date', async ({ request }) => {
    const response = await request.get(
      `${API_BASE}/api/v1/DiaryEntry/GetMinDate/${ww1DiaryId}`,
      { ignoreHTTPSErrors: true },
    )
    expect(response.ok()).toBeTruthy()
    const date = await response.text()
    expect(new Date(JSON.parse(date)).getTime()).not.toBeNaN()
  })

  test('POST create diary entry returns 401 without auth', async ({ request }) => {
    const response = await request.post(`${API_BASE}/api/v1/DiaryEntry/Create`, {
      ignoreHTTPSErrors: true,
      data: { diaryId: ww1DiaryId, date: new Date().toISOString(), location: 'Test', entry: 'Test entry' },
    })
    expect(response.status()).toBe(401)
  })
})
