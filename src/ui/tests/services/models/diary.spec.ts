import Diary from '@/services/models/diary'
import { describe, expect, it } from 'vitest'

describe('Diary Model', () => {
  it('Create Diary Model', async () => {
    // Arrange
    const diaryId : string = crypto.randomUUID()
    const title : string = 'Title'
    const author : string = 'Author'
    const description : string = 'Description'

    // Act
    const diary : Diary = new Diary(title, author, description, diaryId)

    // Assert
    expect(diary.title).equal(title)
    expect(diary.author).equal(author)
    expect(diary.description).equal(description)
    expect(diary.diaryId).equal(diaryId)
  })

  it('Change Diary Model', async () => {
    // Arrange
    const diaryId : string = crypto.randomUUID()
    const title : string = 'Title'
    const author : string = 'Author'
    const description : string = 'Description'
    const diary : Diary = new Diary(title, author, description, diaryId)

    // Act
    const newDiaryId : string = crypto.randomUUID()
    diary.diaryId = newDiaryId
    const newTitle = 'New Diary Title'
    diary.title = newTitle
    const newAuthor = 'New Author'
    diary.author = newAuthor
    const newDescription = 'New Description'
    diary.description = newDescription

    // Assert
    expect(diary.title).equal(newTitle)
    expect(diary.author).equal(newAuthor)
    expect(diary.description).equal(newDescription)
    expect(diary.diaryId).equal(newDiaryId)
  })
})
