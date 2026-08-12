// <copyright file="DiaryEntryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;

    public class DiaryEntryService : IDiaryEntryService
    {
        private readonly DiaryDatabaseContext _context;

        public DiaryEntryService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public async Task<DiaryEntryDTO> CreateDiaryEntryAsync(DiaryEntryDTO diaryEntry)
        {
            if (diaryEntry.Date == null || diaryEntry.Date == DateTime.MinValue)
            {
                throw new ArgumentException($"Date has to be not null and greater than {DateTime.MinValue}.");
            }

            _context.Add(diaryEntry);
            await _context.SaveChangesAsync();
            return diaryEntry;
        }

        public async Task DeleteDiaryEntryAsync(DiaryEntryDTO diaryEntry)
        {
            _context.Remove(diaryEntry);
            await _context.SaveChangesAsync();
        }

        public async Task<DiaryDateRange> GetDiaryDateRangeAsync(Guid diaryId)
        {
            var maxDate = await _context.DiaryEntries
                .Where(d => d.DiaryId == diaryId)
                .OrderByDescending(d => d.Date)
                .Select(d => d.Date)
                .FirstOrDefaultAsync() ?? DateTime.MaxValue;

            var minDate = await _context.DiaryEntries
                .Where(d => d.DiaryId == diaryId)
                .OrderBy(d => d.Date)
                .Select(d => d.Date)
                .FirstOrDefaultAsync() ?? DateTime.MinValue;

            return new DiaryDateRange { MaxDateTime = maxDate, MinDateTime = minDate };
        }

        public async Task<List<int>> SearchDiaryEntriesAsync(Guid diaryId, DateTime from, DateTime until, SearchType searchType, int utcOffsetMinutes = 0)
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

            // The projection runs client-side: only the Where is translated to SQL.
            var matches = await _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId && x.Date >= from && x.Date <= until)
                .ToListAsync();

            return matches
                .OrderBy(func)
                .Select(func)
                .Distinct()
                .ToList();
        }

        public async Task<List<DiaryEntryDTO>> GetDiaryEntriesAsync(Guid diaryId)
        {
            return await _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .OrderBy(x => x.Date)
                .ToListAsync();
        }

        public async Task<List<DiaryEntryDTO>> GetDiaryEntriesAsync(Guid diaryId, DateTime from, DateTime until)
        {
            return await _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId && x.Date >= from && x.Date <= until)
                .OrderBy(x => x.Date)
                .ToListAsync();
        }

        public async Task<DiaryEntryDTO?> GetDiaryEntryAsync(Guid id)
        {
            return await _context.DiaryEntries.Where(x => x.DiaryEntryId == id)
                .FirstOrDefaultAsync();
        }

        public async Task<DiaryEntryDTO> UpdateDiaryEntryAsync(DiaryEntryDTO diaryEntry)
        {
            _context.Update(diaryEntry);
            await _context.SaveChangesAsync();
            return diaryEntry;
        }

        public async Task<PagedResultDTO<DiaryEntryDTO>> TextSearchDiaryEntriesAsync(Guid diaryId, string search, int page = 1, int pageSize = 20)
        {
            var query = _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .Where(x => x.Entry.Contains(search) ||
                            x.Location.Contains(search) ||
                            (x.FromLocation != null && x.FromLocation.Contains(search)) ||
                            (x.ToLocation != null && x.ToLocation.Contains(search)));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Date)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResultDTO<DiaryEntryDTO>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<DateTime> MinDiaryEntryDateAsync(Guid diaryId)
        {
            if (!await _context.DiaryEntries.AnyAsync(x => x.DiaryId == diaryId))
            {
                return DateTime.UtcNow;
            }

            return await _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .MinAsync(x => x.Date) ?? DateTime.MinValue;
        }

        public async Task<DateTime> MaxDiaryEntryDateAsync(Guid diaryId)
        {
            if (!await _context.DiaryEntries.AnyAsync(x => x.DiaryId == diaryId))
            {
                return DateTime.UtcNow;
            }

            return await _context.DiaryEntries
                .Where(x => x.DiaryId == diaryId)
                .MaxAsync(x => x.Date) ?? DateTime.MaxValue;
        }
    }
}
