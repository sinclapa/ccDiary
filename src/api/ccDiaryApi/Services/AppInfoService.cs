// <copyright file="AppInfoService.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Services
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using Microsoft.EntityFrameworkCore;

    public class AppInfoService : IAppInfoService
    {
        private readonly DiaryDatabaseContext _context;

        public AppInfoService(DiaryDatabaseContext context)
        {
            _context = context;
        }

        public async Task<AppInfoDTO?> GetAppInfoAsync()
        {
            return await _context.AppInfo.SingleOrDefaultAsync(a => a.Id == 1);
        }
    }
}
