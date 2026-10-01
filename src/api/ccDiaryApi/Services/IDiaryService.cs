// <copyright file="IDiaryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryService
    {
        /// <summary>Lists diaries, searched, sorted and paged.</summary>
        /// <param name="page">The 1-based page.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="search">Text to match in the title or description.</param>
        /// <param name="canSeeHidden">
        /// Which hidden diaries to include. Null by default, which leaves every hidden diary out, so
        /// a caller has to decide to show any.
        /// </param>
        /// <returns>The page, with the total count of matching diaries.</returns>
        Task<PagedResultDto<DiaryDto>> GetDiariesAsync(int page, int pageSize, string? search = null, Func<DiaryDto, bool>? canSeeHidden = null);

        Task<DiaryDto?> GetDiaryAsync(Guid diaryId);

        Task<DiaryDto> CreateAsync(DiaryDto diary);

        Task<DiaryDto> UpdateAsync(DiaryDto diary);

        Task DeleteAsync(DiaryDto diary);
    }
}
