import Diary from '@/services/models/diary'
import { getAppConfigField } from '@/utils/appConfig'

export default class DiaryAPIService {
  async createDiary (diary: Diary) : Promise<Diary | null> {
    const api = new URL('v1/Diary/Create', getAppConfigField('VITE_API'))
    const request = {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(diary),
    }
    let output : Diary | null = null
    await fetch(api, request)
      .then(response => response.json())
      .then(data => { output = data as Diary })
    return output
  }

  async updateDiary (diary: Diary) : Promise<Diary | null> {
    const api = new URL('v1/Diary/Update', getAppConfigField('VITE_API'))
    const request = {
      method: 'PUT',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(diary),
    }
    let output : Diary | null = null
    await fetch(api, request)
      .then(response => response.json())
      .then(data => { output = data as Diary })
    return output
  }

  async deleteDiary (diaryId: string) : Promise<boolean> {
    const api = new URL(`v1/Diary/Delete/${diaryId}`, getAppConfigField('VITE_API'))
    const request = {
      method: 'DELETE',
    }
    let output : boolean = false
    await fetch(api, request)
      .then(response => { output = response.ok })
    return output
  }

  async getDiaries () : Promise<Diary[]> {
    const api = new URL('v1/Diary/Get', getAppConfigField('VITE_API'))
    let output : Diary[] = []
    await fetch(api)
      .then(response => response.json())
      .then(data => output = data as Diary[])
    return output
  }

  async getDiary (diaryId: string) : Promise<Diary | undefined> {
    const api = new URL(`v1/Diary/Get/${diaryId}`, getAppConfigField('VITE_API'))
    let output : Diary | undefined
    await fetch(api)
      .then(response => response.json())
      .then(data => output = data as Diary)
    return output
  }
}

export const diaryAPI = new DiaryAPIService()
