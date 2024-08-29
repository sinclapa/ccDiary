export interface DiaryInterface {
  diaryId?: string;
  title: string;
  author: string;
  description: string
}

export default class Diary implements DiaryInterface {
  diaryId?: string
  title: string
  author: string
  description: string

  constructor (title: string, author: string, description: string, diaryId?: string) {
    this.diaryId = diaryId
    this.title = title
    this.author = author
    this.description = description
  }
}
