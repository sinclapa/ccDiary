export interface DiaryEntryInterface {
  diaryEntryId?: string;
  diaryId: string;
  date: Date;
  location: string;
  entry: string
}

export default class DiaryEntry implements DiaryEntryInterface {
  diaryEntryId?: string
  diaryId: string
  date: Date
  location: string
  entry: string

  constructor (diaryId: string, date: Date, location: string, entry: string, diaryEntryId?: string) {
    this.diaryEntryId = diaryEntryId
    this.diaryId = diaryId
    this.date = date
    this.location = location
    this.entry = entry
  }
}
