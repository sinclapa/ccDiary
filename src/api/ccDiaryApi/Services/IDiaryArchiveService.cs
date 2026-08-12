// <copyright file="IDiaryArchiveService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryArchiveService
    {
        Task<DiaryArchiveDTO?> ExportAsync(Guid diaryId);

        Task<DiaryDTO> ImportAsync(DiaryArchiveDTO diaryArchive);
    }
}
