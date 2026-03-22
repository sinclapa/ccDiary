// <copyright file="DiaryDatabaseMigrationManager.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Migration
{
    using ccDiaryApi.Data.Context;
    using ccDiaryApi.Data.Model;
    using ccDiaryApi.Utilities;
    using Microsoft.EntityFrameworkCore;

    public static class DiaryDatabaseMigrationManager
    {
        public static IHost MigrateDatabase(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                using var appContext = scope.ServiceProvider.GetRequiredService<DiaryDatabaseContext>();
                if (appContext.Database.IsRelational())
                {
                    if (appContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
                    {
                        appContext.Database.EnsureCreated();
                    }
                    else
                    {
                        appContext.Database.Migrate();
                    }
                }

                UpdateAppInfo(appContext);
            }

            return host;
        }

        private static void UpdateAppInfo(DiaryDatabaseContext context)
        {
            var version = AssemblyVersionInfo.GetInformationalVersion();
            var appInfo = context.AppInfo.SingleOrDefault(a => a.Id == 1);
            if (appInfo == null)
            {
                context.AppInfo.Add(new AppInfoDTO
                {
                    Id = 1,
                    InformationalVersion = version,
                    DatabaseLastUpdated = DateTime.UtcNow,
                });
            }
            else
            {
                appInfo.InformationalVersion = version;
                appInfo.DatabaseLastUpdated = DateTime.UtcNow;
            }

            context.SaveChanges();
        }
    }
}
