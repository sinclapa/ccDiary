// <copyright file="DiaryArchiveService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;

    public class DiaryArchiveService : IDiaryArchiveService
    {
        private readonly DiaryDatabaseContext _context;

        public DiaryArchiveService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public async Task<DiaryArchiveDTO?> ExportAsync(Guid diaryId)
        {
            var diary = await _context.Diaries.Where(x => x.DiaryId == diaryId).FirstOrDefaultAsync();
            if (diary == null)
            {
                return null;
            }

            var diaryEntries = await _context.DiaryEntries.Where(x => x.DiaryId == diaryId).OrderBy(x => x.Date).ToListAsync();
            return new DiaryArchiveDTO { Diary = diary, DiaryEntries = diaryEntries };
        }

        public async Task<DiaryDTO> ImportAsync(DiaryArchiveDTO diaryArchive)
        {
            var diary = await _context.Diaries.Where(x => x.DiaryId == diaryArchive.Diary.DiaryId).FirstOrDefaultAsync();

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
                var updateDiaryEntry = await _context.DiaryEntries.Where(x => x.DiaryEntryId == diaryEntry.DiaryEntryId).FirstOrDefaultAsync();
                if (updateDiaryEntry == null)
                {
                    _context.Add(diaryEntry);
                }
                else
                {
                    updateDiaryEntry.Date = diaryEntry.Date;
                    updateDiaryEntry.Location = diaryEntry.Location;
                    updateDiaryEntry.Entry = diaryEntry.Entry;
                    updateDiaryEntry.ShowMap = diaryEntry.ShowMap;
                    updateDiaryEntry.MapLocation = diaryEntry.MapLocation;
                    updateDiaryEntry.ShowJourney = diaryEntry.ShowJourney;
                    updateDiaryEntry.FromLocation = diaryEntry.FromLocation;
                    updateDiaryEntry.ToLocation = diaryEntry.ToLocation;
                    updateDiaryEntry.JourneyMode = diaryEntry.JourneyMode;
                    updateDiaryEntry.ImageData = diaryEntry.ImageData;
                    updateDiaryEntry.ImageContentType = diaryEntry.ImageContentType;
                    _context.Update(updateDiaryEntry);
                }
            }

            await _context.SaveChangesAsync();
            return diaryArchive.Diary;
        }
    }
}
