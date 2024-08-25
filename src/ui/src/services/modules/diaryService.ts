import Diary from '@/services/models/diary'

export default class DiaryAPIService {

  async createDiary(diary: Diary) : Promise<Diary | null> {
    const api = new URL('v1/Diary/Create', import.meta.env.VITE_API)
    const request = {
      method: 'POST',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(diary)
    }
    let output : Diary | null = null
    await fetch(api, request)
      .then(response => response.json())
      .then(data => output = data as Diary)
    return output;
  }

  async updateDiary(diary: Diary) : Promise<Diary | null> {
    const api = new URL('v1/Diary/Update', import.meta.env.VITE_API)
    const request = {
      method: 'PUT',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(diary)
    }
    let output : Diary | null = null
    await fetch(api, request)
      .then(response => response.json())
      .then(data => output = data as Diary)
    return output;
  }

  async deleteDiary(diaryId: string) : Promise<boolean> {
    const api = new URL('v1/Diary/Delete/' + diaryId, import.meta.env.VITE_API)
    const request = {
      method: 'DELETE'
    }
    let output : boolean = false
    await fetch(api, request)
      .then(response => response.ok ? output = true : output = false)
    return output;
  }

  async getDiaries() : Promise<Diary[]> {
    const api = new URL('v1/Diary/Get', import.meta.env.VITE_API)
    let output : Diary[] = []
    await fetch(api)
      .then(response => response.json())
      .then(data => output = data as Diary[])
    return output;
  }
}

export const diaryAPI = new DiaryAPIService();
