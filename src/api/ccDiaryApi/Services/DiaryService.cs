// <copyright file="DiaryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;

    public class DiaryService : IDiaryService
    {
        private readonly DiaryDatabaseContext _context;

        public DiaryService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public async Task<DiaryDTO> CreateAsync(DiaryDTO diary)
        {
            _context.Add(diary);
            await _context.SaveChangesAsync();
            return diary;
        }

        public async Task DeleteAsync(DiaryDTO diary)
        {
            _context.Remove(diary);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResultDTO<DiaryDTO>> GetDiariesAsync(int page, int pageSize, string? search = null)
        {
            var query = _context.Diaries.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => x.Title.Contains(search) ||
                                         (x.Description != null && x.Description.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Author).ThenBy(x => x.Title)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedResultDTO<DiaryDTO>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<DiaryDTO?> GetDiaryAsync(Guid diaryId)
        {
            var diary = await _context.Diaries
                .SingleOrDefaultAsync(x => x.DiaryId == diaryId);
            return diary;
        }

        public async Task<DiaryDTO> UpdateAsync(DiaryDTO diary)
        {
            _context.Update(diary);
            await _context.SaveChangesAsync();
            return diary;
        }
    }
}
