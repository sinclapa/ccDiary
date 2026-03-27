// <copyright file="DiaryDatabaseMigrationManager.cs" company="CookingCode">
// Copyright (c) CookingCode. All rights reserved.
// </copyright>

namespace ccDiaryApi.Data.Migration
{
    using System.Diagnostics.CodeAnalysis;
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
                    ApplyRelationalMigration(appContext);
                }

                UpdateAppInfo(appContext);
            }

            return host;
        }

        // SQL Server migration branch requires a live SQL Server and cannot be tested in CI.
        // SQLite path is covered by integration tests via CustomWebApplicationFactory.
        [ExcludeFromCodeCoverage(Justification = "SQL Server Migrate() path requires a live SQL Server instance; not testable in unit/integration tests.")]
        private static void ApplyRelationalMigration(DiaryDatabaseContext appContext)
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
