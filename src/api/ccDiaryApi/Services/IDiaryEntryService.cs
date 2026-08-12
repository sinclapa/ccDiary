// <copyright file="IDiaryEntryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryEntryService
    {
        Task<List<int>> SearchDiaryEntriesAsync(Guid diaryId, DateTime from, DateTime until, SearchType searchType, int utcOffsetMinutes = 0);

        Task<List<DiaryEntryDTO>> GetDiaryEntriesAsync(Guid diaryId, DateTime from, DateTime until);

        Task<List<DiaryEntryDTO>> GetDiaryEntriesAsync(Guid diaryId);

        Task<DiaryEntryDTO?> GetDiaryEntryAsync(Guid id);

        Task<DiaryDateRange> GetDiaryDateRangeAsync(Guid diaryId);

        Task DeleteDiaryEntryAsync(DiaryEntryDTO diaryEntry);

        Task<DiaryEntryDTO> CreateDiaryEntryAsync(DiaryEntryDTO diaryEntry);

        Task<DiaryEntryDTO> UpdateDiaryEntryAsync(DiaryEntryDTO diaryEntry);

        Task<DateTime> MinDiaryEntryDateAsync(Guid diaryId);

        Task<DateTime> MaxDiaryEntryDateAsync(Guid diaryId);

        Task<PagedResultDTO<DiaryEntryDTO>> TextSearchDiaryEntriesAsync(Guid diaryId, string search, int page = 1, int pageSize = 20);
    }
}
