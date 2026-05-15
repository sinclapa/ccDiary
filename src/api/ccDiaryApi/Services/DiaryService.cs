// <copyright file="DiaryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;

    public class DiaryService : IDiaryService
    {
        private readonly DiaryDatabaseContext _context;

        public DiaryService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public DiaryDTO Create(DiaryDTO diary)
        {
            _context.Add(diary);
            _context.SaveChanges();
            return diary;
        }

        public void Delete(DiaryDTO diary)
        {
            _context.Remove(diary);
            _context.SaveChanges();
        }

        public PagedResultDTO<DiaryDTO> GetDiaries(int page, int pageSize)
        {
            var query = _context.Diaries
                .OrderBy(x => x.Author).ThenBy(x => x.Title);
            var totalCount = query.Count();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PagedResultDTO<DiaryDTO>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public DiaryDTO? GetDiary(Guid diaryId)
        {
            var diary = _context.Diaries
                .SingleOrDefault(x => x.DiaryId == diaryId);
            return diary;
        }

        public DiaryDTO Update(DiaryDTO diary)
        {
            _context.Update(diary);
            _context.SaveChanges();
            return diary;
        }
    }
}
