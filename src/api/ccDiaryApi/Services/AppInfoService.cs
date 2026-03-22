// <copyright file="AppInfoService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;

    public class AppInfoService : IAppInfoService
    {
        private readonly DiaryDatabaseContext _context;

        public AppInfoService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public AppInfoDTO? GetAppInfo()
        {
            return _context.AppInfo.SingleOrDefault(a => a.Id == 1);
        }
    }
}
