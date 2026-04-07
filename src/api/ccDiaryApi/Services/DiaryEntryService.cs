// <copyright file="DiaryEntryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;

    public class DiaryEntryService : IDiaryEntryService
    {
        private readonly DiaryDatabaseContext _context;

        public DiaryEntryService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public DiaryEntryDTO CreateDiaryEntry(DiaryEntryDTO diaryEntry)
        {
            if (diaryEntry.Date == null || diaryEntry.Date == DateTime.MinValue)
            {
                throw new ArgumentException($"Date has to be not null and greater than {DateTime.MinValue}.");
            }

            _context.Add(diaryEntry);
            _context.SaveChanges();
            return diaryEntry;
        }

        public void DeleteDiaryEntry(DiaryEntryDTO diaryEntry)
        {
            _context.Remove(diaryEntry);
            _context.SaveChanges();
        }

        public DiaryDateRange GetDiaryDateRange(Guid diaryId)
        {
            var maxDate = _context.DiaryEntries
                .Where(d => d.DiaryId == diaryId)
                .OrderByDescending(d => d.Date)
                .Select(d => d.Date).AsEnumerable()
                .FirstOrDefault() ?? DateTime.MaxValue;

            var minDate = _context.DiaryEntries
                .Where(d => d.DiaryId == diaryId)
                .OrderBy(d => d.Date)
                .Select(d => d.Date).AsEnumerable()
                .FirstOrDefault() ?? DateTime.MinValue;

            return new DiaryDateRange { MaxDateTime = maxDate, MinDateTime = minDate };
        }

        public List<int> SearchDiaryEntries(Guid diaryId, DateTime from, DateTime until, SearchType searchType, int utcOffsetMinutes = 0)
        {
            Func<DiaryEntryDTO, int> func;
            switch (searchType)
            {
                case SearchType.Year:
                    func = new Func<DiaryEntryDTO, int>(x => x.Date.GetValueOrDefault().AddMinutes(utcOffsetMinutes).Year);
                    break;
                case SearchType.Month:
                    func = new Func<DiaryEntryDTO, int>(x => x.Date.GetValueOrDefault().AddMinutes(utcOffsetMinutes).Month);
                    break;
                case SearchType.Day:
                    func = new Func<DiaryEntryDTO, int>(x => x.Date.GetValueOrDefault().AddMinutes(utcOffsetMinutes).Day);
                    break;
                default:
                    throw new ArgumentException($"Unhandled SearchType [{searchType}]");
            }

            return _context.DiaryEntries.Where(x => x.DiaryId == diaryId && x.Date >= from && x.Date <= until)
                .OrderBy(func)
                .Select(func)
                .Distinct()
                .ToList();
        }

        public List<DiaryEntryDTO> GetDiaryEntries(Guid diaryId)
        {
            return _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .OrderBy(x => x.Date)
                .ToList();
        }

        public List<DiaryEntryDTO> GetDiaryEntries(Guid diaryId, DateTime from, DateTime until)
        {
            return _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId && x.Date >= from && x.Date <= until)
                .OrderBy(x => x.Date)
                .ToList();
        }

        public DiaryEntryDTO? GetDiaryEntry(Guid id)
        {
            return _context.DiaryEntries.Where(x => x.DiaryEntryId == id)
                .FirstOrDefault();
        }

        public DiaryEntryDTO UpdateDiaryEntry(DiaryEntryDTO diaryEntry)
        {
            _context.Update(diaryEntry);
            _context.SaveChanges();
            return diaryEntry;
        }

        public DateTime MinDiaryEntryDate(Guid diaryId)
        {
            if (!_context.DiaryEntries.Any(x => x.DiaryId == diaryId))
            {
                return DateTime.UtcNow;
            }

            return _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .Min(x => x.Date) ?? DateTime.MinValue;
        }

        public DateTime MaxDiaryEntryDate(Guid diaryId)
        {
            if (!_context.DiaryEntries.Any(x => x.DiaryId == diaryId))
            {
                return DateTime.UtcNow;
            }

            return _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .Max(x => x.Date) ?? DateTime.MaxValue;
        }
    }
}