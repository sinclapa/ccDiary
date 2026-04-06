export interface DiaryEntryInterface {
  diaryEntryId?: string;
  diaryId: string;
  date: Date;
  location: string;
  entry: string;
  mapLocation: string;
  showMap: boolean;
}

export default class DiaryEntry implements DiaryEntryInterface {
  diaryEntryId?: string
  diaryId: string
  date: Date
  location: string
  entry: string
  mapLocation: string
  showMap: boolean

  constructor (diaryId: string, date: Date, location: string, entry: string, diaryEntryId?: string, mapLocation = '', showMap = false) {
    this.diaryEntryId = diaryEntryId
    this.diaryId = diaryId
    this.date = date
    this.location = location
    this.entry = entry
    this.mapLocation = mapLocation
    this.showMap = showMap
  }
}
