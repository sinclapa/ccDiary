import DiaryEntry from '@/services/models/diaryEntry'
import dayjs from 'dayjs'

export default class DiaryEntryAPIService {
  async createDiaryEntry (diaryEntry: DiaryEntry) : Promise<DiaryEntry | null> {
    const api = new URL('v1/DiaryEntry/Create', import.meta.env.VITE_API)
    const request = {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(diaryEntry),
    }
    let output : DiaryEntry | null = null
    await fetch(api, request)
      .then(response => response.json())
      .then(data => output = data as DiaryEntry)
    return output
  }

  async searchDiaryEntry (diaryId: string, year?: number, month?: number) : Promise<number[] | null> {
    let api = new URL(`v1/DiaryEntry/Search/${diaryId}/`, import.meta.env.VITE_API)
    if (year !== undefined) {
      api = new URL(`${year}/`, api)
    }
    if (month !== undefined) {
      api = new URL(`${month}/`, api)
    }
    let output : number[] | null = null
    await fetch(api)
      .then(response => response.json())
      .then(data => output = data as number[])
    return output
  }

  async searchDiaryEntryForDay (diaryId: string, year: number, month: number, day: number) : Promise<DiaryEntry[]> {
    const utcOffsetMinutes : number = dayjs(new Date(year, month, day)).utcOffset()
    const api = new URL(`v1/DiaryEntry/Search/${diaryId}/${year}/${month}/${day}`, import.meta.env.VITE_API)
    let output : DiaryEntry[] = []
    const request = {
      headers: {
        'x-utc-offset': `${utcOffsetMinutes}`,
      },
    }
    await fetch(api, request)
      .then(response => response.json())
      .then(data => output = data as DiaryEntry[])
    return output.map(x => new DiaryEntry(x.diaryId, new Date(x.date), x.location, x.entry, x.diaryEntryId))
  }

  async getMinDate (diaryId: string) : Promise<Date> {
    const api = new URL(`v1/DiaryEntry/GetMinDate/${diaryId}`, import.meta.env.VITE_API)
    let output : Date = new Date(0, 0, 1)
    await fetch(api)
      .then(response => response.json())
      .then(data => output = new Date(data))
    return output
  }

  async getMaxDate (diaryId: string) : Promise<Date> {
    const api = new URL(`v1/DiaryEntry/GetMaxDate/${diaryId}`, import.meta.env.VITE_API)
    let output : Date = new Date(9999, 0, 1)
    await fetch(api)
      .then(response => response.json())
      .then(data => output = new Date(data))
    return output
  }

  async updateDiaryEntry (diaryEntry: DiaryEntry) : Promise<DiaryEntry | null> {
    const api = new URL('v1/DiaryEntry/Update', import.meta.env.VITE_API)
    const request = {
      method: 'PUT',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(diaryEntry),
    }
    let output : DiaryEntry | null = null
    await fetch(api, request)
      .then(response => response.json())
      .then(data => output = data as DiaryEntry)
    return output
  }

  async deleteDiaryEntry (diaryEntryId: string) : Promise<boolean> {
    const api = new URL(`v1/DiaryEntry/Delete/${diaryEntryId}`, import.meta.env.VITE_API)
    const request = {
      method: 'DELETE',
    }
    let output : boolean = false
    await fetch(api, request)
      .then(response => response.ok ? output = true : output = false)
    return output
  }
}

export const diaryEntryAPI = new DiaryEntryAPIService()
