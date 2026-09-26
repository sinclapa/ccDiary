export type JourneyMode = 'crow-flies' | 'walking' | 'car' | 'train' | 'boat'

/** The most images one entry may hold; the API rejects more. */
export const MAX_ENTRY_IMAGES = 10

export interface DiaryEntryImage {
  data: string;
  contentType: string;
}

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
  journeyMode: JourneyMode;
  images: DiaryEntryImage[];
}

/**
 * An entry's images from an API payload. The API repeats the first image in `imageData` for
 * callers that predate `images`; a payload with only that field is a one-image entry.
 */
export function imagesFrom (payload: { images?: DiaryEntryImage[] | null, imageData?: string | null, imageContentType?: string | null }): DiaryEntryImage[] {
  if (payload.images) {
    return payload.images
  }
  return payload.imageData && payload.imageContentType
    ? [{ data: payload.imageData, contentType: payload.imageContentType }]
    : []
}

export function imageSrc (image: DiaryEntryImage) {
  return `data:${image.contentType};base64,${image.data}`
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
  journeyMode: JourneyMode
  images: DiaryEntryImage[]

  constructor (diaryId: string, date: Date, location: string, entry: string, options?: {
    diaryEntryId?: string
    mapLocation?: string
    showMap?: boolean
    fromLocation?: string
    toLocation?: string
    showJourney?: boolean
    journeyMode?: JourneyMode
    images?: DiaryEntryImage[]
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
    this.journeyMode = options?.journeyMode ?? 'crow-flies'
    this.images = options?.images ?? []
  }
}
