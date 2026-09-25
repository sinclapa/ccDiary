import DiaryEntry, { imagesFrom } from '@/services/models/diaryEntry'
import PagedResult from '@/services/models/pagedResult'
import { apiFetch, apiFetchJson, apiUrl } from '@/services/modules/apiClient'
import dayjs from 'dayjs'
import { ENTRY_UTC_OFFSET_MINUTES } from '@/utils/entryTime'

const jsonHeaders = {
  Accept: 'application/json',
  'Content-Type': 'application/json',
}

export default class DiaryEntryAPIService {
  async createDiaryEntry (diaryEntry: DiaryEntry) : Promise<DiaryEntry | null> {
    return apiFetchJson<DiaryEntry | null>(apiUrl('v1/DiaryEntry/Create'), null, {
      method: 'POST',
      headers: jsonHeaders,
      body: JSON.stringify(diaryEntry),
    })
  }

  async searchDiaryEntry (diaryId: string, year?: number, month?: number) : Promise<number[] | null> {
    let api = apiUrl(`v1/DiaryEntry/Search/${diaryId}/`)
    if (year !== undefined) {
      api = new URL(`${year}/`, api)
    }
    if (month !== undefined) {
      api = new URL(`${month}/`, api)
    }
    let requestInit: RequestInit | undefined
    if (year !== undefined && month !== undefined) {
      const utcOffsetMinutes = ENTRY_UTC_OFFSET_MINUTES
      requestInit = { headers: { 'x-utc-offset': `${utcOffsetMinutes}` } }
    }
    return apiFetchJson<number[] | null>(api, null, requestInit)
  }

  async searchDiaryEntryForDay (diaryId: string, year: number, month: number, day: number) : Promise<DiaryEntry[]> {
    const utcOffsetMinutes : number = ENTRY_UTC_OFFSET_MINUTES
    const api = apiUrl(`v1/DiaryEntry/Search/${diaryId}/${year}/${month}/${day}`)
    const output = await apiFetchJson<(DiaryEntry & { imageData?: string, imageContentType?: string })[]>(api, [], {
      headers: {
        'x-utc-offset': `${utcOffsetMinutes}`,
      },
    })
    return output.map(x => new DiaryEntry(x.diaryId, new Date(x.date), x.location, x.entry, {
      diaryEntryId: x.diaryEntryId,
      mapLocation: x.mapLocation ?? '',
      showMap: x.showMap ?? false,
      fromLocation: x.fromLocation ?? '',
      toLocation: x.toLocation ?? '',
      showJourney: x.showJourney ?? false,
      journeyMode: x.journeyMode ?? 'crow-flies',
      images: imagesFrom(x),
    }))
  }

  async getMinDate (diaryId: string) : Promise<Date> {
    const data = await apiFetchJson<string | null>(apiUrl(`v1/DiaryEntry/GetMinDate/${diaryId}`), null)
    return data === null
      ? dayjs(new Date(0, 0, 1)).startOf('day').toDate()
      : dayjs(data).startOf('day').toDate()
  }

  async getMaxDate (diaryId: string) : Promise<Date> {
    const data = await apiFetchJson<string | null>(apiUrl(`v1/DiaryEntry/GetMaxDate/${diaryId}`), null)
    return data === null
      ? dayjs(new Date(9999, 0, 1)).endOf('day').toDate()
      : dayjs(data).endOf('day').toDate()
  }

  async updateDiaryEntry (diaryEntry: DiaryEntry) : Promise<DiaryEntry | null> {
    return apiFetchJson<DiaryEntry | null>(apiUrl('v1/DiaryEntry/Update'), null, {
      method: 'PUT',
      headers: jsonHeaders,
      body: JSON.stringify(diaryEntry),
    })
  }

  async textSearchDiaryEntries (diaryId: string, search: string, page: number = 1, pageSize: number = 20) : Promise<PagedResult<DiaryEntry>> {
    const api = apiUrl(`v1/DiaryEntry/TextSearch/${diaryId}`)
    api.searchParams.set('search', search)
    api.searchParams.set('page', String(page))
    api.searchParams.set('pageSize', String(pageSize))
    return apiFetchJson<PagedResult<DiaryEntry>>(api, { items: [], totalCount: 0, page, pageSize })
  }

  async deleteDiaryEntry (diaryEntryId: string) : Promise<boolean> {
    const response = await apiFetch(apiUrl(`v1/DiaryEntry/Delete/${diaryEntryId}`), { method: 'DELETE' })
    return response.ok
  }
}

export const diaryEntryAPI = new DiaryEntryAPIService()
