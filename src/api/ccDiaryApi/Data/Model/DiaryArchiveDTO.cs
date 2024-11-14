// <copyright file="DiaryArchiveDTO.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    public class DiaryArchiveDTO
    {
        required public DiaryDTO Diary { get; set; }

        required public List<DiaryEntryDTO> DiaryEntries { get; set; }
    }
}
