export interface DiaryEntryInterface {
  diaryEntryId?: string;
  diaryId: string;
  date: Date;
  location: string;
  entry: string;
  mapLocation: string;
  showMap: boolean;
  fromLocation: string;
  toLocation: string;
  showJourney: boolean;
  imageData?: string;
  imageContentType?: string;
}

export default class DiaryEntry implements DiaryEntryInterface {
  diaryEntryId?: string
  diaryId: string
  date: Date
  location: string
  entry: string
  mapLocation: string
  showMap: boolean
  fromLocation: string
  toLocation: string
  showJourney: boolean
  imageData?: string
  imageContentType?: string

  constructor (diaryId: string, date: Date, location: string, entry: string, options?: {
    diaryEntryId?: string
    mapLocation?: string
    showMap?: boolean
    fromLocation?: string
    toLocation?: string
    showJourney?: boolean
    imageData?: string
    imageContentType?: string
  }) {
    this.diaryEntryId = options?.diaryEntryId
    this.diaryId = diaryId
    this.date = date
    this.location = location
    this.entry = entry
    this.mapLocation = options?.mapLocation ?? ''
    this.showMap = options?.showMap ?? false
    this.fromLocation = options?.fromLocation ?? ''
    this.toLocation = options?.toLocation ?? ''
    this.showJourney = options?.showJourney ?? false
    this.imageData = options?.imageData
    this.imageContentType = options?.imageContentType
  }
}
