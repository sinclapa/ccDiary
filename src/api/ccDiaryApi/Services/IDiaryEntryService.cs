// <copyright file="IDiaryEntryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryEntryService
    {
        Task<List<int>> SearchDiaryEntriesAsync(Guid diaryId, DateTime from, DateTime until, SearchType searchType, int utcOffsetMinutes = 0);

        Task<List<DiaryEntryDto>> GetDiaryEntriesAsync(Guid diaryId, DateTime from, DateTime until);

        Task<List<DiaryEntryDto>> GetDiaryEntriesAsync(Guid diaryId);

        Task<DiaryEntryDto?> GetDiaryEntryAsync(Guid id);

        Task<DiaryDateRange> GetDiaryDateRangeAsync(Guid diaryId);

        Task DeleteDiaryEntryAsync(DiaryEntryDto diaryEntry);

        Task<DiaryEntryDto> CreateDiaryEntryAsync(DiaryEntryDto diaryEntry);

        Task<DiaryEntryDto> UpdateDiaryEntryAsync(DiaryEntryDto diaryEntry);

        Task<DateTime> MinDiaryEntryDateAsync(Guid diaryId);

        Task<DateTime> MaxDiaryEntryDateAsync(Guid diaryId);

        Task<PagedResultDto<DiaryEntryDto>> TextSearchDiaryEntriesAsync(Guid diaryId, string search, int page = 1, int pageSize = 20);
    }
}
