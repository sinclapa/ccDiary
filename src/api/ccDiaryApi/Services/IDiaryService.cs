// <copyright file="IDiaryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryService
    {
        Task<PagedResultDto<DiaryDto>> GetDiariesAsync(int page, int pageSize, string? search = null);

        Task<DiaryDto?> GetDiaryAsync(Guid diaryId);

        Task<DiaryDto> CreateAsync(DiaryDto diary);

        Task<DiaryDto> UpdateAsync(DiaryDto diary);

        Task DeleteAsync(DiaryDto diary);
    }
}
