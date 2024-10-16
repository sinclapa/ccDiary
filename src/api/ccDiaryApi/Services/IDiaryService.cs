// <copyright file="IDiaryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryService
    {
        IEnumerable<DiaryDTO> Get();

        DiaryDTO? Get(Guid diaryId);

        DiaryDTO Create(DiaryDTO diary);

        DiaryDTO Update(DiaryDTO diary);

        void Delete(DiaryDTO diary);
    }
}
