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
            if (diaryEntry.Date == DateTime.MinValue)
            {
                throw new ApplicationException($"Date has to be not null and greater than {DateTime.MinValue}.");
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
                .FirstOrDefault(DateTime.MaxValue);

            var minDate = _context.DiaryEntries
                .Where(d => d.DiaryId == diaryId)
                .OrderBy(d => d.Date)
                .Select(d => d.Date).AsEnumerable()
                .FirstOrDefault(DateTime.MinValue);

            return new DiaryDateRange { maxDateTime = maxDate, minDateTime = minDate };
        }

        public List<int> SearchDiaryEntries(Guid diaryId, DateTime from, DateTime to, SearchType searchType)
        {
            Func<DiaryEntryDTO, int> func;
            switch (searchType)
            {
                case SearchType.Year:
                    func = new Func<DiaryEntryDTO, int>(x => x.Date.Year);
                    break;
                case SearchType.Month:
                    func = new Func<DiaryEntryDTO, int>(x => x.Date.Month);
                    break;
                case SearchType.Day:
                    func = new Func<DiaryEntryDTO, int>(x => x.Date.Day);
                    break;
                default:
                    throw new ApplicationException($"Unhandled SearchType [{searchType}]");
            }

            return _context.DiaryEntries.Where(x => x.DiaryId == diaryId && x.Date >= from && x.Date <= to)
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

        public List<DiaryEntryDTO> GetDiaryEntries(Guid diaryId, DateTime from, DateTime to)
        {
            return _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId && x.Date >= from && x.Date <= to)
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
            if (diaryEntry.Date == DateTime.MinValue)
            {
                throw new ApplicationException($"Date has to be not null and greater than {DateTime.MinValue}.");
            }

            _context.Update(diaryEntry);
            _context.SaveChanges();
            return diaryEntry;
        }

        public DateTime MinDiaryEntryDate(Guid diaryId)
        {
            if (_context.DiaryEntries.Count(x => x.DiaryId == diaryId) == 0)
            {
                return DateTime.UtcNow;
            }

            return _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .Min(x => x.Date);
        }

        public DateTime MaxDiaryEntryDate(Guid diaryId)
        {
            if (_context.DiaryEntries.Count(x => x.DiaryId == diaryId) == 0)
            {
                return DateTime.UtcNow;
            }

            return _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .Max(x => x.Date);
        }
    }
}