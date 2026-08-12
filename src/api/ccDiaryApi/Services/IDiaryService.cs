// <copyright file="IDiaryService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryService
    {
        Task<PagedResultDTO<DiaryDTO>> GetDiariesAsync(int page, int pageSize, string? search = null);

        Task<DiaryDTO?> GetDiaryAsync(Guid diaryId);

        Task<DiaryDTO> CreateAsync(DiaryDTO diary);

        Task<DiaryDTO> UpdateAsync(DiaryDTO diary);

        Task DeleteAsync(DiaryDTO diary);
    }
}
