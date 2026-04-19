// <copyright file="AppRole.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Model
{
    public enum AppRole
    {
        /// <summary>Diary administrator with full access.</summary>
        DiaryAdmin = 0,

        /// <summary>Diary contributor with write access to owned diaries.</summary>
        DiaryContributor = 1,
    }
}
