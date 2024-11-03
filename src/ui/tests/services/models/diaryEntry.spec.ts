import DiaryEntry from '@/services/models/diaryEntry'
import { describe, expect, it } from 'vitest'

describe('DiaryEntry Model', () => {

  it('Create DiaryEntry Model', async () => {
    // Arrange
    const diaryId : string = crypto.randomUUID()
    const date : Date = new Date()
    const location : string = "My Diary Location"
    const entry : string = "My Diary Entry"
    const diaryEntryId : string = crypto.randomUUID()

    // Act
    let diaryEntry : DiaryEntry = new DiaryEntry(diaryId, date, location, entry, diaryEntryId)

    // Assert
    expect(diaryEntry.date).equal(date);
    expect(diaryEntry.diaryEntryId).equal(diaryEntryId);
    expect(diaryEntry.diaryId).equal(diaryId);
    expect(diaryEntry.entry).equal(entry);
    expect(diaryEntry.location).equal(location);
  })

  it('Change DiaryEntry Model', async () => {
    // Arrange
    const diaryId : string = crypto.randomUUID()
    const date : Date = new Date()
    const location : string = "My Diary Location"
    const entry : string = "My Diary Entry"
    const diaryEntryId : string = crypto.randomUUID()
    let diaryEntry : DiaryEntry = new DiaryEntry(diaryId, date, location, entry, diaryEntryId)

    // Act
    const newDiaryId : string = crypto.randomUUID()
    diaryEntry.diaryId = newDiaryId;
    const newDate : Date = new Date(2020, 10, 28, 17, 30, 0, 0)
    diaryEntry.date = newDate
    const newLocation : string = "My New Diary Location"
    diaryEntry.location = newLocation
    const newEntry : string = "My New Diary Entry"
    diaryEntry.entry = newEntry
    const newDiaryEntryId : string = crypto.randomUUID()
    diaryEntry.diaryEntryId = newDiaryEntryId

    // Assert
    expect(diaryEntry.date).equal(newDate);
    expect(diaryEntry.diaryEntryId).equal(newDiaryEntryId);
    expect(diaryEntry.diaryId).equal(newDiaryId);
    expect(diaryEntry.entry).equal(newEntry);
    expect(diaryEntry.location).equal(newLocation);
  })
})
