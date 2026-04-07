// <copyright file="IDiaryEntryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryEntryService
    {
        List<int> SearchDiaryEntries(Guid diaryId, DateTime from, DateTime until, SearchType searchType, int utcOffsetMinutes = 0);

        List<DiaryEntryDTO> GetDiaryEntries(Guid diaryId, DateTime from, DateTime until);

        List<DiaryEntryDTO> GetDiaryEntries(Guid diaryId);

        DiaryEntryDTO? GetDiaryEntry(Guid id);

        DiaryDateRange GetDiaryDateRange(Guid diaryId);

        void DeleteDiaryEntry(DiaryEntryDTO diaryEntry);

        DiaryEntryDTO CreateDiaryEntry(DiaryEntryDTO diaryEntry);

        DiaryEntryDTO UpdateDiaryEntry(DiaryEntryDTO diaryEntry);

        DateTime MinDiaryEntryDate(Guid diaryId);

        DateTime MaxDiaryEntryDate(Guid diaryId);
    }
}
