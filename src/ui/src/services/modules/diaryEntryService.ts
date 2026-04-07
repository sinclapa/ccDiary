import DiaryEntry from '@/services/models/diaryEntry'
import { getAppConfigField } from '@/utils/appConfig'
import dayjs from 'dayjs'

export default class DiaryEntryAPIService {
  async createDiaryEntry (diaryEntry: DiaryEntry) : Promise<DiaryEntry | null> {
    const api = new URL('v1/DiaryEntry/Create', getAppConfigField('VITE_API'))
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
    let api = new URL(`v1/DiaryEntry/Search/${diaryId}/`, getAppConfigField('VITE_API'))
    if (year !== undefined) {
      api = new URL(`${year}/`, api)
    }
    if (month !== undefined) {
      api = new URL(`${month}/`, api)
    }
    let requestInit: RequestInit | undefined
    if (year !== undefined && month !== undefined) {
      const utcOffsetMinutes = dayjs(new Date(year, month - 1, 1)).utcOffset()
      requestInit = { headers: { 'x-utc-offset': `${utcOffsetMinutes}` } }
    }
    let output : number[] | null = null
    await (requestInit !== undefined ? fetch(api, requestInit) : fetch(api))
      .then(response => response.json())
      .then(data => output = data as number[])
    return output
  }

  async searchDiaryEntryForDay (diaryId: string, year: number, month: number, day: number) : Promise<DiaryEntry[]> {
    const utcOffsetMinutes : number = dayjs(new Date(year, month, day)).utcOffset()
    const api = new URL(`v1/DiaryEntry/Search/${diaryId}/${year}/${month}/${day}`, getAppConfigField('VITE_API'))
    let output : DiaryEntry[] = []
    const request = {
      headers: {
        'x-utc-offset': `${utcOffsetMinutes}`,
      },
    }
    await fetch(api, request)
      .then(response => response.json())
      .then(data => output = data as DiaryEntry[])
    return output.map(x => new DiaryEntry(x.diaryId, new Date(x.date), x.location, x.entry, x.diaryEntryId, x.mapLocation ?? '', x.showMap ?? false, x.fromLocation ?? '', x.toLocation ?? '', x.showJourney ?? false, x.imageData, x.imageContentType))
  }

  async getMinDate (diaryId: string) : Promise<Date> {
    const api = new URL(`v1/DiaryEntry/GetMinDate/${diaryId}`, getAppConfigField('VITE_API'))
    let output : Date = dayjs(new Date(0, 0, 1)).startOf('day').toDate()
    await fetch(api)
      .then(response => response.json())
      .then(data => output = dayjs(data).startOf('day').toDate())
    return output
  }

  async getMaxDate (diaryId: string) : Promise<Date> {
    const api = new URL(`v1/DiaryEntry/GetMaxDate/${diaryId}`, getAppConfigField('VITE_API'))
    let output : Date = dayjs(new Date(9999, 0, 1)).endOf('day').toDate()
    await fetch(api)
      .then(response => response.json())
      .then(data => output = dayjs(data).endOf('day').toDate())
    return output
  }

  async updateDiaryEntry (diaryEntry: DiaryEntry) : Promise<DiaryEntry | null> {
    const api = new URL('v1/DiaryEntry/Update', getAppConfigField('VITE_API'))
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
    const api = new URL(`v1/DiaryEntry/Delete/${diaryEntryId}`, getAppConfigField('VITE_API'))
    const request = {
      method: 'DELETE',
    }
    let output : boolean = false
    await fetch(api, request)
      .then(response => { output = response.ok })
    return output
  }
}

export const diaryEntryAPI = new DiaryEntryAPIService()
