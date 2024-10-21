import { diaryAPI } from '@/services/modules/diaryService'
import Diary from '@/services/models/diary'
import { afterAll, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'

function createFetchResponse(data: any) {
  return { json: () => new Promise((resolve) => resolve(data)) }
}


describe('pages/diaries/index.vue with successful HTTP Get', () => {
  const realFetch = global.fetch
  beforeAll(() => {
    const diaryGetResponse = [
      {
        "diaryId": "0af38239-b24f-4fa9-f679-08dcc87078fb",
        "title": "Test Diary",
        "author": "A J Smith",
        "description": "First Test Diary"
      },
      {
        "diaryId": "f80a9774-ab8c-44fd-f67d-08dcc87078fb",
        "title": "80 Days Around the World",
        "author": "Jules Verne",
        "description": "Circumnavigation around the earth"
      },
      {
        "diaryId": "ca89c5cf-7699-4d1c-f67b-08dcc87078fb",
        "title": "To the Moon and Back",
        "author": "Tom Hanks",
        "description": "Filming Apollo 13"
      }
    ]
    global.fetch = vi.fn().mockResolvedValue(createFetchResponse(diaryGetResponse))

  })

  afterAll(() => {
    global.fetch = realFetch
  })

  beforeEach(() => {
    vi.stubEnv('VITE_API', 'http://localhost')
  })

  it('Get Diaries', async () => {
    const results = await diaryAPI.getDiaries()
    expect(global.fetch).toBeCalledTimes(1)
  })
})
