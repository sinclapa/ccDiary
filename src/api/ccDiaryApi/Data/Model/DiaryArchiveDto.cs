// <copyright file="DiaryArchiveDto.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    public class DiaryArchiveDto
    {
        required public DiaryDto Diary { get; set; }

        required public List<DiaryEntryDto> DiaryEntries { get; set; }
    }
}
