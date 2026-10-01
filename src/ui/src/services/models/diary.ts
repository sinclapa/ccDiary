export interface DiaryInterface {
  diaryId?: string;
  title: string;
  author: string;
  description: string;
  ownerId?: string;
  isHidden?: boolean;
}

export default class Diary implements DiaryInterface {
  diaryId?: string
  title: string
  author: string
  description: string
  ownerId?: string
  isHidden?: boolean

  constructor (title: string, author: string, description: string, diaryId?: string, ownerId?: string) {
    this.diaryId = diaryId
    this.title = title
    this.author = author
    this.description = description
    this.ownerId = ownerId
  }
}
