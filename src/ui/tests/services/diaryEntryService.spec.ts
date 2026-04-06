import DiaryEntry from '@/services/models/diaryEntry'
import { diaryEntryAPI } from '@/services/modules/diaryEntryService'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import dayjs from 'dayjs'

const baseUrl : string = 'http://localhost'

describe('DiaryEntry Service', () => {
  beforeEach(() => {
    vi.stubEnv('VITE_API', baseUrl)
  })

  it('Get Diary Entry MinDate', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    await diaryEntryAPI.getMinDate(diaryId)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/GetMinDate/${diaryId}`, baseUrl))
  })

  it('Get Diary Entry MaxDate', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    await diaryEntryAPI.getMaxDate(diaryId)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/GetMaxDate/${diaryId}`, baseUrl))
  })

  it('Search Diary Entry', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    await diaryEntryAPI.searchDiaryEntry(diaryId)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Search/${diaryId}/`, baseUrl))
  })

  it('Search Diary Entry for Year', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    await diaryEntryAPI.searchDiaryEntry(diaryId, 2024)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Search/${diaryId}/2024/`, baseUrl))
  })

  it('Search Diary Entry for Year and Month', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    await diaryEntryAPI.searchDiaryEntry(diaryId, 2024, 9)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Search/${diaryId}/2024/9/`, baseUrl))
  })

  it('Search Diary Entry for Year, Month and Day', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ([]),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    await diaryEntryAPI.searchDiaryEntryForDay(diaryId, 2024, 9, 17)

    // Assert
    const utcOffsetMinutes : number = dayjs(new Date(2024, 9, 17)).utcOffset()
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Search/${diaryId}/2024/9/17`, baseUrl),
      {
        headers: {
          'x-utc-offset': `${utcOffsetMinutes}`,
        },
      }
    )
  })

  it('searchDiaryEntryForDay maps mapLocation and showMap from API response', async () => {
    // Arrange
    const diaryId = crypto.randomUUID()
    const entryId = crypto.randomUUID()
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ([{
        diaryId,
        diaryEntryId: entryId,
        date: new Date(2024, 8, 17).toISOString(),
        location: 'Home',
        entry: 'A note',
        mapLocation: 'London, UK',
        showMap: true,
      }]),
    } as Response)

    // Act
    const results = await diaryEntryAPI.searchDiaryEntryForDay(diaryId, 2024, 9, 17)

    // Assert
    expect(results).toHaveLength(1)
    expect(results[0].mapLocation).toBe('London, UK')
    expect(results[0].showMap).toBe(true)
  })

  it('searchDiaryEntryForDay defaults mapLocation to empty string when null', async () => {
    // Arrange
    const diaryId = crypto.randomUUID()
    vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ([{
        diaryId,
        diaryEntryId: crypto.randomUUID(),
        date: new Date(2024, 8, 17).toISOString(),
        location: 'Home',
        entry: 'A note',
        mapLocation: null,
        showMap: false,
      }]),
    } as Response)

    // Act
    const results = await diaryEntryAPI.searchDiaryEntryForDay(diaryId, 2024, 9, 17)

    // Assert
    expect(results[0].mapLocation).toBe('')
    expect(results[0].showMap).toBe(false)
  })

  it('Delete DiaryEntry', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryEntryId = crypto.randomUUID()
    const result = await diaryEntryAPI.deleteDiaryEntry(diaryEntryId)

    // Assert
    expect(result).toBe(true)
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Delete/${diaryEntryId}`, baseUrl), { method: 'DELETE' })
  })

  it('Delete DiaryEntry Fail', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: false,
      statusText: 'Not Found',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryEntryId = crypto.randomUUID()
    const result = await diaryEntryAPI.deleteDiaryEntry(diaryEntryId)

    // Assert
    expect(result).toBe(false)
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Delete/${diaryEntryId}`, baseUrl), { method: 'DELETE' })
  })

  it('Create DiaryEntry', async () => {
    // Arrange
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      statusText: 'OK',
      json: async () => ({}),
    } as Response)

    // Act
    const diaryId = crypto.randomUUID()
    const diaryEntry : DiaryEntry = {
      date: new Date(),
      diaryId,
      location: 'TestLocation',
      entry: 'TestEntry',
      mapLocation: '',
      showMap: false,
    }
    await diaryEntryAPI.createDiaryEntry(diaryEntry)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Create`, baseUrl),
      {
        body: JSON.stringify(diaryEntry),
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
    const diaryId = crypto.randomUUID()
    const diaryEntry : DiaryEntry = {
      date: new Date(),
      diaryId,
      location: 'TestLocation',
      entry: 'TestEntry',
      mapLocation: '',
      showMap: false,
    }
    await diaryEntryAPI.updateDiaryEntry(diaryEntry)

    // Assert
    expect(fetchSpy).toHaveBeenCalledWith(new URL(`v1/DiaryEntry/Update`, baseUrl),
      {
        body: JSON.stringify(diaryEntry),
        headers: {
          Accept: 'application/json',
          'Content-Type': 'application/json',
        },
        method: 'PUT',
      }
    )
  })
})
