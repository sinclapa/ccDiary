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

        public IEnumerable<DiaryDTO> Get()
        {
            var diaries = _context.Diaries
                .OrderBy(x => x.Author).ThenBy(x => x.Title);
            return diaries;
        }

        public DiaryDTO? Get(Guid diaryId)
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
