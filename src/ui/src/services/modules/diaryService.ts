import Diary from '@/services/models/diary'
import PagedResult from '@/services/models/pagedResult'
import { apiFetch, apiFetchJson, apiUrl } from '@/services/modules/apiClient'

const jsonHeaders = {
  Accept: 'application/json',
  'Content-Type': 'application/json',
}

export default class DiaryAPIService {
  async createDiary (diary: Diary) : Promise<Diary | null> {
    return apiFetchJson<Diary | null>(apiUrl('v1/Diary/Create'), null, {
      method: 'POST',
      headers: jsonHeaders,
      body: JSON.stringify(diary),
    })
  }

  async updateDiary (diary: Diary) : Promise<Diary | null> {
    return apiFetchJson<Diary | null>(apiUrl('v1/Diary/Update'), null, {
      method: 'PUT',
      headers: jsonHeaders,
      body: JSON.stringify(diary),
    })
  }

  async deleteDiary (diaryId: string) : Promise<boolean> {
    const response = await apiFetch(apiUrl(`v1/Diary/Delete/${diaryId}`), { method: 'DELETE' })
    return response.ok
  }

  async getDiaries (page: number = 1, pageSize: number = 12, search?: string) : Promise<PagedResult<Diary>> {
    const api = apiUrl('v1/Diary/Get')
    api.searchParams.set('page', String(page))
    api.searchParams.set('pageSize', String(pageSize))
    if (search) api.searchParams.set('search', search)
    return apiFetchJson<PagedResult<Diary>>(api, { items: [], totalCount: 0, page, pageSize })
  }

  async getDiary (diaryId: string) : Promise<Diary | undefined> {
    return apiFetchJson<Diary | undefined>(apiUrl(`v1/Diary/Get/${diaryId}`), undefined)
  }
}

export const diaryAPI = new DiaryAPIService()
