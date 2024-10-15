// <copyright file="DiaryDatabaseMigrationManager.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Migration
{
    using ccDiaryApi.Data.Context;
    using Microsoft.EntityFrameworkCore;

    public static class DiaryDatabaseMigrationManager
    {
        public static IHost MigrateDatabase(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                using var appContext = scope.ServiceProvider.GetRequiredService<DiaryDatabaseContext>();
                if (appContext.Database.IsRelational() && appContext.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
                {
                    appContext.Database.Migrate();
                }

                appContext.Database.EnsureCreated();
            }

            return host;
        }
    }
}
