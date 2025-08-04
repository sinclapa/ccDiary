import Diary from '@/services/models/diary'
import { diaryAPI } from '@/services/modules/diaryService'
import { beforeEach, describe, expect, it, vi } from 'vitest'

const baseUrl : string = 'http://localhost'

describe('Diary Service', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_API', baseUrl)
  })

  it('Get Diaries', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    await diaryAPI.getDiaries()

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL('v1/Diary/Get', baseUrl))
  })

  it('Get Diary', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    await diaryAPI.getDiary(diaryId)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/Diary/Get/${diaryId}`, baseUrl))
  })

  it('Delete Diary', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    const result = await diaryAPI.deleteDiary(diaryId)

    // Assert
    expect(result).toBe(true)
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/Diary/Delete/${diaryId}`, baseUrl), { method: 'DELETE' })
  })

  it('Delete Diary Fail', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      statusText: 'Not Found',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    const result = await diaryAPI.deleteDiary(diaryId)

    // Assert
    expect(result).toBe(false)
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/Diary/Delete/${diaryId}`, baseUrl), { method: 'DELETE' })
  })

  it('Create Diary', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diary : Diary = {
      author: 'TestAuthor',
      description: 'TestDescription',
      title: 'TestTitle',
    }
    await diaryAPI.createDiary(diary)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/Diary/Create`, baseUrl),
      {
        body: JSON.stringify(diary),
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
        method: 'POST',
      }
    )
  })

  it('Update  Diary', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diary : Diary = {
      author: 'TestAuthor',
      description: 'TestDescription',
      title: 'TestTitle',
    }
    await diaryAPI.updateDiary(diary)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/Diary/Update`, baseUrl),
      {
        body: JSON.stringify(diary),
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
        method: 'PUT',
      }
    )
  })
})
