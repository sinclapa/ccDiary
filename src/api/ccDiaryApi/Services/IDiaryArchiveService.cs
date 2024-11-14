// <copyright file="IDiaryArchiveService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IDiaryArchiveService
    {
        DiaryDTO Import(DiaryArchiveDTO diaryArchive);
    }
}
