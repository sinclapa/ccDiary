// <copyright file="DiaryArchiveService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;

    public class DiaryArchiveService : IDiaryArchiveService
    {
        private readonly DiaryDatabaseContext _context;

        public DiaryArchiveService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public DiaryDTO Import(DiaryArchiveDTO diaryArchive)
        {
            var diary = _context.Diaries.Where(x => x.DiaryId == diaryArchive.Diary.DiaryId).FirstOrDefault();

            if (diary == null)
            {
                _context.Add(diaryArchive.Diary);
            }
            else
            {
                diary.Title = diaryArchive.Diary.Title;
                diary.Author = diaryArchive.Diary.Author;
                diary.Description = diaryArchive.Diary.Description;
                _context.Update(diary);
            }

            foreach (var diaryEntry in diaryArchive.DiaryEntries)
            {
                var updateDiaryEntry = _context.DiaryEntries.Where(x => x.DiaryEntryId == diaryEntry.DiaryEntryId).FirstOrDefault();
                if (updateDiaryEntry == null)
                {
                    _context.Add(diaryEntry);
                }
                else
                {
                    updateDiaryEntry.Date = diaryEntry.Date;
                    updateDiaryEntry.Location = diaryEntry.Location;
                    updateDiaryEntry.Entry = diaryEntry.Entry;
                    _context.Update(updateDiaryEntry);
                }
            }

            _context.SaveChanges();


            return diaryArchive.Diary;
        }
    }
}
