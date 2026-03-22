// <copyright file="IAppInfoService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Model;

    public interface IAppInfoService
    {
        AppInfoDTO? GetAppInfo();
    }
}
